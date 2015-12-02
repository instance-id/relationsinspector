using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using RelationsInspector.Extensions;
using RelationsInspector.Tweening;
using System;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Collections;

namespace RelationsInspector
{
    internal class Workspace<T,P> : IWorkspace, IViewParent<T,P> where T : class
	{
        Graph<T,P> graph;
		IGraphView<T,P> view;
        IGraphBackendInternal<T,P> graphBackend;
		Rect drawRect;
		LayoutType layoutType;
		LayoutType defaultLayoutType = LayoutType.Graph;
		HashSet<T> seedEntities; 
		TweenCollection graphPosTweens;
        bool hasGraphPosChanges;

        //layout
        bool layoutRunning = true;  // we can't use layoutEnumerator to determine if layout is in progress, because it is set too late
        IEnumerator layoutEnumerator;
        Stopwatch layoutTimer = new Stopwatch();
		float nextVertexPosTweenUpdate;

        Action Repaint;
        Action<Action> Exec;
        RelationsInspectorAPI API;
        RNG builderRNG;

        enum AdjustTransformMode { Not, Smooth, Instant }; // how to adjust the view transform to layout (vertex position) changes
        AdjustTransformMode adjustTransformMode = AdjustTransformMode.Instant;

		static Dictionary<LayoutType, GUIContent> layoutButtonContent = new Dictionary<LayoutType, GUIContent>()
		{
			{ LayoutType.Graph, new GUIContent("Graph", "Use graph layout") },
			{ LayoutType.Tree, new GUIContent("Tree", "Use tree layout") }
		};

		internal Workspace(Type backendType, object[] targets, RelationsInspectorAPI API, Action Repaint, Action<Action> Exec)
		{
            this.Repaint = Repaint;
            this.Exec = Exec;
            this.API = API;
            this.graphBackend = (IGraphBackendInternal<T, P>) BackendUtil.CreateBackendDecorator(backendType); 

            // create new layout params, they are not comming from the cfg yet
			this.layoutType = (LayoutType) GUIUtil.GetPrefsInt(GetPrefsKeyLayout(), (int)defaultLayoutType);			
			this.graphPosTweens = new TweenCollection();

            this.seedEntities = graphBackend.Init( targets, API ).ToHashSet();
            this.builderRNG = new RNG( 4 ); // chosen by fair dice role. guaranteed to be random.

            // when targets is null, show the toolbar only. don't create a graph (and view)
            // when rootEntities is empty, create graph and view anyway, so the user can add entities
            if ( targets != null )
                InitGraph();
		}

		string GetPrefsKeyLayout()
		{
			return System.IO.Path.Combine(graphBackend.GetType().ToString(), "LayoutType");
		}

		void InitGraph()
		{
            graph = GraphBuilder<T, P>.Build(seedEntities, graphBackend.GetRelations, builderRNG, Settings.Instance.maxGraphNodes);
            if (graph == null)
                return;

            // delay the view construction, so this.drawRect can be set before that runs.
            Exec( () => view = new IMView<T, P>( graph, this ) );

            // run layout, unless the user wants to use caches and a cache could be loaded
            bool runLayout = Settings.Instance.cacheLayouts ?
                !GraphPosSerialization.LoadGraphLayout( graph, seedEntities, graphBackend.GetDecoratedType() ) :
                true;

            if ( runLayout )
            {
                layoutRunning = true;
                // make the view focus on the initial graph unfolding
                adjustTransformMode = AdjustTransformMode.Instant;
                Exec( DoAutoLayout );
            }
        }

		public void Update()
		{
			UpdateLayout();
			graphPosTweens.Update();

			bool doRepaint = false;
            hasGraphPosChanges = graphPosTweens.HasChanges;
            doRepaint |= hasGraphPosChanges;
            doRepaint |= Tweener.gen.HasChanges;

			if(doRepaint)
				Repaint();
		}

		void UpdateLayout()
		{
            if ( !layoutRunning )
                return;

            // enumerator may not exist yet
            if ( layoutEnumerator == null )
                return;

			layoutTimer.Reset();
			layoutTimer.Start();
			bool generatorActive;
            var parms = Settings.Instance.layoutTweenParameters;
			do
			{
				generatorActive = layoutEnumerator.MoveNext();
			} while (generatorActive && (layoutTimer.ElapsedTicks/(float)Stopwatch.Frequency) < parms.maxFrameDuration);

			var positions = layoutEnumerator.Current as Dictionary<T, Vector2>;
			bool gotFinalPositions = !generatorActive;
            if ( !generatorActive )
                layoutRunning = false;

			var time = EditorApplication.timeSinceStartup;
			if (gotFinalPositions || time > nextVertexPosTweenUpdate)
			{
				nextVertexPosTweenUpdate = (float)time + parms.vertexPosTweenUpdateInterval;
				if( positions != null)
				{
					foreach (var pair in positions)
					{
						VertexData<T, P> vData = null;
                        if (graph.VerticesData.TryGetValue(pair.Key, out vData))
                        {
                            T tweenOwner = pair.Key;
                            var evalFunc = TweenUtil.GetCombinedEasing(tweenOwner, graphPosTweens, vData.pos, pair.Value);
                            graphPosTweens.Replace( tweenOwner, new Tween<Vector2>(v => vData.pos = v, parms.vertexPosTweenDuration, evalFunc) );
                        }
                    }
				}
			}
		}

        public void Relayout()
        {
            adjustTransformMode = AdjustTransformMode.Smooth;
            Exec( DoAutoLayout );
        }

		public void OnGUI(Rect drawRect)
		{					
			this.drawRect = drawRect;

			if (view != null)
			{
                if ( hasGraphPosChanges && adjustTransformMode != AdjustTransformMode.Not )
                {
                    bool instant = adjustTransformMode == AdjustTransformMode.Instant;
                    view.FitViewRectToGraph( instant );
                }
				
				try
				{
					view.Draw();
				}
				catch (MissingReferenceException)
				{
					graph.CleanNullRefs();
				}

				view.HandleEvent(Event.current);
			}
		}

		public void OnToolbarGUI()
		{		
			EditorGUI.BeginChangeCheck();

            if ( graph != null && GUILayout.Button( "Relayout", EditorStyles.miniButton, GUILayout.ExpandWidth( false ) ) )
                Exec( () => API.Relayout() );

            // let user pick a layout type (iff tree layout is an option)
            if (graph != null && graph.IsTree())
            {
                layoutType = (LayoutType)GUIUtil.EnumToolbar("", layoutType, (t) => layoutButtonContent[(LayoutType)t], EditorStyles.miniButton);
                if (EditorGUI.EndChangeCheck())
                {
                    GUIUtil.SetPrefsInt(GetPrefsKeyLayout(), (int)layoutType);
                    adjustTransformMode = AdjustTransformMode.Smooth;
                    Exec( DoAutoLayout );
                }
            }

            // draw view controls
            if (view != null)
			{
				GUILayout.FlexibleSpace();
				view.OnToolbarGUI();
			}
		}

		public Rect OnControlsGUI()
		{
			if (graphBackend != null)
				return graphBackend.OnGUI( );

			return GUILayoutUtility.GetRect(0, 0, new[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) });
		}

		public void OnSelectionChange()
		{
            if (graphBackend != null)
                graphBackend.OnUnitySelectionChange();
		}

        public void OnEvent(Event e)
        {
            if ( graphBackend != null && 
                e != null && 
                e.type == EventType.ExecuteCommand &&
                !string.IsNullOrEmpty(e.commandName) )
            {
                graphBackend.OnCommand( e.commandName );
                e.Use();
            }
        }

        public void OnDestroy()
        {
            if ( graph != null && !layoutRunning && Settings.Instance.cacheLayouts )
                GraphPosSerialization.SaveGraphLayout(graph, seedEntities, graphBackend.GetDecoratedType());
            graphBackend.OnDestroy();
        }

		void DoAutoLayout()
		{
			if (graph == null)
				return;

            layoutRunning = true;
            layoutEnumerator = GraphLayout<T, P>.Run(graph, layoutType, Settings.Instance.layoutTweenParameters);
		}

        #region implementing IWorkspace

        void IWorkspace.AddTargets( object[] targetsToAdd, Vector2 pos )
        {
            if ( targetsToAdd == null )
            {
                Log.Error( "Targets are null" );
                return;
            }

            var asT = targetsToAdd.OfType<T>().ToArray();
            if ( asT.Count() != targetsToAdd.Count() )
            {
                Log.Error( "Not all targets are of type " + typeof( T ) );
                return;
            }

            if ( graph == null )
                return;

            seedEntities.UnionWith( asT );

            // when no position is given, use screen center
            if ( pos == Vector2.zero )
                pos = (view == null ) ? Vector2.zero : view.GetGraphPosition( drawRect.center );
            else // transform pos to graph space
                pos = (view == null) ? Vector2.zero : view.GetGraphPosition( pos );

            GraphBuilder<T, P>.Append( graph, asT, pos, graphBackend.GetRelations, builderRNG, graph.VertexCount + Settings.Instance.maxGraphNodes );
            adjustTransformMode = AdjustTransformMode.Not;  // don't mess with the user's transform settings
            Exec( DoAutoLayout );
        }

		void IWorkspace.AddEntity(object entity, Vector2 position)
		{
            if ( entity == null )
            {
                Log.Error( "Entity is null" );
                return;
            }

			var asT = entity as T;
            if ( asT == null )
            {
                Log.Error( string.Format("Can't add entity {0}. Backend type {1} is not assignable from its type {2}", entity, typeof( T ), entity.GetType() ) );
                return;
            }

            if ( graph == null )
            {
                Log.Error( "Graph is missing" );
                return;
            }

            if ( graph.ContainsVertex( asT ) )
            {
                Log.Message( "Graph already contains entity: " + asT);
                return;
            }

            if(position == Vector2.zero)
                position = view.GetGraphPosition( drawRect.center );

            graph.AddVertex( asT, position );
		}

		void IWorkspace.RemoveEntity(object entityObj)
		{
			var entity = entityObj as T;
            if ( entity == null )
            {
                Log.Error( "Can't remove entity: it is not of type " + typeof( T ) );
                return;
            }

            if ( graph == null )
                return;

            if ( !graph.ContainsVertex( entity ) )
            {
                Log.Error( "Can't remove entity: it is not part of the graph." );
                return;
            }

            graph.RemoveVertex( entity );

			if (view != null)
				view.OnRemovedEntity(entity);
		}

        void IWorkspace.ExpandEntity( object entityObj )
        {
            var entity = entityObj as T;
            if ( entity == null )
            {
                Log.Error( "Can't expand entity: it is not of type " + typeof( T ) );
                return;
            }

            if ( graph == null )
                return;

            GraphBuilder<T, P>.Expand( graph, entity, graphBackend.GetRelations, builderRNG, graph.VertexCount + Settings.Instance.maxGraphNodes );
            adjustTransformMode = AdjustTransformMode.Not;  // don't mess with the user's transform settings
            Exec( DoAutoLayout );
        }

        void IWorkspace.FoldEntity( object entityObj )
        {
            var entity = entityObj as T;
            if ( entity == null )
            {
                Log.Error( "Can't fold entity: it is not part of the graph." );
                return;
            }

            if(graph != null )
                FoldUtil.Fold( graph, entity, IsSeed );
        }

		void IWorkspace.CreateRelation(object[] sourceEntities)
		{
            // validate source entities
            if ( sourceEntities == null )
            {
                Log.Error( "Can't create relations: entities are missing." );
                return;
            }

            var asT = sourceEntities.OfType<T>();
            if ( asT.Count() != sourceEntities.Count() )
            {
                Log.Error("Can't create relations: not all entities are of type " + typeof( T ) );
                return;
            }

            if ( view != null )
                view.CreateEdge(asT);
		}

		void IWorkspace.AddRelation(object sourceObj, object targetObj, object tagObj)
		{
            // validate source
			var source = sourceObj as T;
            if ( source == null )
            {
                Log.Error( "Can't add relation: source entity is not of type " + typeof( T ) );
                return;
            }

            // validate target
			var target = targetObj as T;
            if ( target == null )
            {
                Log.Error( "Can't add relation: target entity is not of type " + typeof( T ) );
                return;
            }

            // validate tag
            if ( tagObj == null )
            {
                Log.Error( "Can't add relations: tag is null" );
                return;
            }

            P tag;
			try
            {
				tag = (P)tagObj;
			}
            catch (InvalidCastException)
            {
                Log.Error(string.Format( "Can't add relation. Tag is of type {0}, expected {1}", tagObj.GetType(), typeof( P ) ) );
                return;
			}

            if ( graph != null )
			    graph.AddEdge( new Relation<T, P>( source, target, tag ) );
		}

		void IWorkspace.RemoveRelation(object sourceObj, object targetObj, object tagObj)
		{
            // validate source
            var source = sourceObj as T;
            if ( source == null )
            {
                Log.Error( "Can't remove relation: source entity is not of type " + typeof( T ) );
                return;
            }

            // validate target
            var target = targetObj as T;
            if ( target == null )
            {
                Log.Error( "Can't remove relation: target entity is not of type " + typeof( T ) );
                return;
            }

            // validate tag
            if ( tagObj == null )
            {
                Log.Error( "Can't remove relation: tag is null" );
                return;
            }

            P tag;
            try
            {
                tag = (P) tagObj;
            }
            catch ( InvalidCastException )
            {
                Log.Error( string.Format( "Can't add relation. Tag is of type {0}, expected {1}", tagObj.GetType(), typeof( P ) ) );
                return;
            }

            if ( graph == null )
                return;

            var sourceOutEdges = graph.GetOutEdges(source);
			if (sourceOutEdges == null)
			{
				return;
			}
			var matchingEdges = sourceOutEdges.Where(e => e.Matches(source, target, tag));
			var edge = matchingEdges.FirstOrDefault();
			if (edge == null)
				return;

			graph.RemoveEdge(edge);
		}

		void IWorkspace.SelectEntityNodes(System.Predicate<object> doSelect)
		{
            if(view != null )
			    view.SelectEntityNodes(doSelect);
		}

		#endregion

		#region implemention IViewParent

		public void RepaintView()
		{
			Repaint();
		}

		public Rect GetViewRect()
		{
			return drawRect;
		}

		public void MoveEntity(T entity, Vector2 delta)
		{
			Exec( () => MoveEntityCmd(entity, delta) );
		}

		void MoveEntityCmd(T entity, Vector2 delta)
		{
			Vector2 newPos = graph.GetPos(entity) + delta;
			graph.SetPos(entity, newPos);
		}

		public bool IsSeed(T entity)
		{
			return seedEntities.Contains(entity);
		}

		public IGraphBackendInternal<T, P> GetBackend()
		{
			return graphBackend;
		}

        public RelationsInspectorAPI GetAPI()
        {
            return API;
        }

        #endregion
    }
}
