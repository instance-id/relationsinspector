﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RelationsInspector.Extensions;

namespace RelationsInspector
{
	public class Graph<T, P> where T : class
	{
		public Dictionary<T, VertexData<T, P>> VerticesData { get; private set; }
		public IEnumerable<T> Vertices { get { return VerticesData.Keys; } }
		public HashSet<Edge<T, P>> Edges { get; private set; }
		public int VertexCount { get { return VerticesData.Count; } }

		public Graph()
		{
			VerticesData = new Dictionary<T, VertexData<T, P>>();
			Edges = new HashSet<Edge<T, P>>();
		}

		public Graph(Graph<T, P> source)
		{
			this.VerticesData = new Dictionary<T, VertexData<T, P>>(source.VerticesData);
			this.Edges = new HashSet<Edge<T, P>>(source.Edges);
		}

		public void CleanNullRefs()
		{
			VerticesData.RemoveWhere(pair => Util.IsBadRef(pair.Key) );
			Edges.RemoveWhere(edge => Util.IsBadRef(edge.Source) || Util.IsBadRef(edge.Target) );
		}

		public virtual bool AddVertex(T vertex)
		{
			if (vertex == null)
				return false;

			if (VerticesData.ContainsKey(vertex))
				return false;

			VerticesData[vertex] = new VertexData<T, P>(vertex);
			return true;
		}

		public virtual bool RemoveVertex(T vertex)
		{
			if (vertex == null)
				return false;

			var affectedEdges = Edges.Where(e => e.Source == vertex || e.Target == vertex).ToArray();
			foreach (var edge in affectedEdges)
				RemoveEdge(edge);

			if (!VerticesData.Remove(vertex))
				return false;

			return true;
		}

		public virtual bool AddVertex(T vertex, Vector2 position)
		{
			bool ret = AddVertex(vertex);
			if (ret)
				SetPos(vertex, position);
			return ret;
		}

		public virtual bool AddEdge(Edge<T, P> edge)
		{
			if (edge == null || edge.Source == null || edge.Target == null)
				return false;

			if (!VerticesData.ContainsKey(edge.Source) || !VerticesData.ContainsKey(edge.Target))
				return false;

			if (!Edges.Add(edge))
				return false;

			VerticesData[edge.Source].OutEdges.Add(edge);
			VerticesData[edge.Target].InEdges.Add(edge);
			return true;
		}

		public virtual bool RemoveEdge(Edge<T, P> edge)
		{
			if (edge == null)
				return false;

			if (!Edges.Remove(edge))
				return false;

			VerticesData[edge.Source].OutEdges.Remove(edge);
			VerticesData[edge.Target].InEdges.Remove(edge);
			return true;
		}

		public bool IsRoot(T vertex)
		{
			return !VerticesData[vertex].InEdges.Get().Any();
		}

		public bool ContainsVertex(T vertex)
		{
			return VerticesData.ContainsKey(vertex);
		}

		public Vector2 GetPos(T vertex)
		{
			VertexData<T, P> data;
			if (!VerticesData.TryGetValue(vertex, out data))
			{
				Debug.LogError("Graph.GetPos: unknown vertex " + vertex);
				return Vector2.zero;
			}

			return data.pos;
		}

		public void SetPos(T vertex, Vector2 pos)
		{
			VertexData<T, P> data;
			if (!VerticesData.TryGetValue(vertex, out data))
			{
				Debug.LogError("Graph.SetPos: unknown vertex " + vertex);
				return;
			}

			data.pos = pos;
		}
	}
}