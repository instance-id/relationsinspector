using RelationsInspector;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using RelationsInspector.Extensions;
using Type = System.Type;
using System.Collections;

namespace RelationsInspector
{
	internal class Workspace<T,P> : IWorkspace, IViewParent<T,P> where T : class
	{
		GraphWithRoots<T,P> graph;
		IGraphView<T,P> view;
		LayoutParams layoutParams;
		RelationsInspectorWindow editorWindow;	// parent window (for BeginWindows() and EndWindows())
		IGraphBackend<T,P> graphBackend;
		DebugSettings debugSettings;
		Rect minimapRect = new Rect(30, 30, 100, 100);
		Rect drawRect;
		IEnumerator layoutEnumerator;
		LayoutType layoutType;
		LayoutType defaultLayoutType = LayoutType.Graph;
		HashSet<T> rootEntities;

#if DEBUG
		// debug settings
		bool permaRepaint;	// repaint permanently
#endif

		internal Workspace(Type backendType, RelationsInspectorWindow editorWindow, object[] targets)
		{
			this.editorWindow = editorWindow;
			this.graphBackend = (IGraphBackend<T, P>)System.Activator.CreateInstance(backendType, true);

			// create new layout params, they are not comming from the cfg yet
			this.layoutParams = ScriptableObject.CreateInstance<LayoutParams>();
			this.debugSettings = ScriptableObject.CreateInstance<DebugSettings>();
			this.layoutType = (LayoutType) GUIUtil.GetPrefsInt(GetPrefsKeyLayout(), (int)defaultLayoutType);
			rootEntities = new HashSet<T>();

			InitGraph(targets);
		}

		string GetPrefsKeyLayout()
		{
			return System.IO.Path.Combine(graphBackend.GetType().ToString(), "LayoutType");
		}

		void InitGraph(object[] targets)
		{
			rootEntities = graphBackend.Init(targets, editorWindow).ToHashSet();

			// when targets is null, show the toolbar only. don't create a graph (and view)
			// when rootEntities is empty, create graph and view anyway, so the user can add entities
			if (targets == null)
				return;

			graph = GraphBuilder<T, P>.Build(rootEntities, graphBackend.GetRelations, int.MaxValue);
			if (graph != null)
				ExecOnUpdate(DoAutoLayout);
		}

		void ExecOnUpdate( System.Action action )
		{
			editorWindow.ExecOnUpdate( action );
		}

		public void Update()
		{
#if DEBUG
			if (permaRepaint)
				editorWindow.Repaint();
#endif
		}

		public void OnGUI(Rect drawRect)
		{
			this.drawRect = drawRect;

			if (view != null)
			{
				try
				{
					view.Draw();
				}
				catch (MissingReferenceException)
				{
					graph.CleanNullRefs();
				}

				if (graph != null)
				{
					var entityPositions = graph.VerticesData.Values.Select(data => data.pos);
					var style = editorWindow.skin.minimap;
					var newCenter = Minimap.Draw( entityPositions, minimapRect, view.GetViewRect(drawRect), debugSettings.showMinimapGraphBounds, style);
					view.SetCenter(newCenter);
				}

				view.HandleEvent(Event.current);
			}
		}

		public void OnToolbarGUI()
		{
			EditorGUI.BeginChangeCheck();
			layoutType = (LayoutType)GUIUtil.EnumToolbar("Layout", layoutType, EditorStyles.miniButton);
			if (EditorGUI.EndChangeCheck())
			{
				GUIUtil.SetPrefsInt(GetPrefsKeyLayout(), (int)layoutType);
				ExecOnUpdate( DoAutoLayout );
			}
#if DEBUG
			// option to repaint constantly
			permaRepaint = GUILayout.Toggle( permaRepaint, "perma-repaint", GUILayout.ExpandWidth(false));

			// option to run the layout
			if (GUILayout.Button("Run layout", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
			{
				ExecOnUpdate( DoAutoLayout );
			}

			/*
			EditorGUI.BeginDisabledGroup(layoutEnumerator == null);
			if (GUILayout.Button("step", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
			{
				if (!layoutEnumerator.MoveNext())
					layoutEnumerator = null;
			}
			EditorGUI.EndDisabledGroup();
			*/
			
			if (GUILayout.Button("Layout params", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
			{
				var window = EditorWindow.CreateInstance<InspectorWindow>();
				window.objectToInspect = layoutParams;
				window.Show();
			}

#endif
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
			if (view != null)
				view.OnWindowSelectionChange();
		}

		void DoAutoLayout()
		{
			if (graph == null)
				return;

			if (layoutParams == null)
				Debug.LogError("canvas auto-layout: missing params");
			else
			{
				layoutEnumerator = GraphLayout<T, P>.Run(graph, layoutType, layoutParams);
				while (layoutEnumerator.MoveNext());	// iterate until finished
				layoutEnumerator = null;
			}

			view = new IMView<T, P>(graph, this);
		}

		#region implementing IWorkspace

		void IWorkspace.AddEntity(object entity, Vector2 position)
		{
			var asT = entity as T;
			if (asT == null )
				return;

			if(graph != null)
				graph.AddVertex(asT, position);
		}

		void IWorkspace.RemoveEntity(object entityObj)
		{
			var entity = entityObj as T;
			if (entity == null)
				return;

			graph.RemoveVertex(entity);
			if (view != null)
				view.OnRemovedEntity(entity);
		}

		void IWorkspace.CreateEdge(object[] sourceEntities, object tagObj)
		{
			if (sourceEntities == null)
				throw new System.ArgumentException("sourceEntities");

			var tag = (P)tagObj;

			if (view != null)
				view.CreateEdge(sourceEntities.Select(obj => obj as T), tag);
		}

		void IWorkspace.AddEdge(object sourceObj, object targetObj, object tagObj)
		{
			if (graph == null)
				return;

			var source = sourceObj as T;
			var target = targetObj as T;
			
			P tag;
			try{
				tag = (P)tagObj;
			}catch(System.InvalidCastException){
				return;
			}

			if (source == null || target == null)
				return;

			var edge = new Edge<T, P>(source, target, tag);
			graph.AddEdge(edge);
		}

		void IWorkspace.RemoveEdge(object sourceObj, object targetObj, object tagObj)
		{
			if (graph == null) return;

			var source = sourceObj as T;
			var target = targetObj as T;

			P tag;
			try
			{
				tag = (P)tagObj;
			}
			catch (System.InvalidCastException)
			{
				return;
			}

			if (source == null || target == null)
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

		#endregion

		#region implemention IViewParent

		public void RepaintView()
		{
			editorWindow.Repaint();
		}

		public Rect GetViewRect()
		{
			return drawRect;
		}

		public RelationInspectorSkin GetSkin()
		{
			return editorWindow.skin;
		}

		public void MoveEntity(T entity, Vector2 delta)
		{
			ExecOnUpdate(() => MoveEntityCmd(entity, delta));
		}

		void MoveEntityCmd(T entity, Vector2 delta)
		{
			Vector2 newPos = graph.GetPos(entity) + delta;
			graph.SetPos(entity, newPos);
		}

		public bool IsRoot(T entity)
		{
			return rootEntities.Contains(entity);
		}

		public IGraphBackend<T, P> GetBackend()
		{
			return graphBackend;
		}

		#endregion
	}
}