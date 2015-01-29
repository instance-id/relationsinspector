using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RelationsInspector.Extensions;

namespace RelationsInspector
{
	public static class GraphExtensions
	{
		public static IEnumerable<T> GetNeighbors<T, P>(this Graph<T, P> graph, T vertex) where T : class
		{
			return graph.GetParents(vertex).Concat( graph.GetChildren(vertex) );
		}
		
		public static IEnumerable<T> GetChildren<T,P>(this Graph<T, P> graph, T vertex) where T : class
		{
			if (!graph.Vertices.Contains(vertex))
				return Enumerable.Empty<T>();

			return graph.VerticesData[vertex].OutEdges.Get().Select(e => e.Target);
		}

		public static IEnumerable<T> GetParents<T, P>(this Graph<T, P> graph, T vertex) where T : class
		{
			if (!graph.Vertices.Contains(vertex))
				return Enumerable.Empty<T>();

			return graph.VerticesData[vertex].InEdges.Get().Select(e => e.Target);
		}

		public static IEnumerable<Edge<T, P>> GetEdges<T, P>(this Graph<T, P> graph, T vertex) where T : class
		{
			return graph.GetInEdges(vertex).Concat( graph.GetOutEdges(vertex) );
		}

		public static IEnumerable<Edge<T, P>> GetOutEdges<T, P>(this Graph<T, P> graph, T vertex) where T : class
		{
			if (!graph.Vertices.Contains(vertex))
				return Enumerable.Empty<Edge<T, P>>();

			return graph.VerticesData[vertex].OutEdges.Get();
		}

		public static IEnumerable<Edge<T, P>> GetInEdges<T, P>(this Graph<T, P> graph, T vertex) where T : class
		{
			if (!graph.Vertices.Contains(vertex))
				return Enumerable.Empty<Edge<T, P>>();

			return graph.VerticesData[vertex].InEdges.Get();
		}

		public static bool IsTree<T,P>(this Graph<T, P> graph, bool demandSingleRooted) where T : class
		{
			var visited = new HashSet<T>();
			var roots = graph.Vertices.Where(v => graph.IsRoot(v));

			// gotta have roots
			if (!roots.Any())
				return false;

			if (demandSingleRooted && roots.Count() > 1)
				return false;

			// mark all root children as visited (recursively)
			// if any are visited twice, there is a diamod or cycle
			foreach (var root in roots)
			{
				visited.Add(root);
				var children = new Queue<T>(graph.GetChildren(root));
				while (children.Any())
				{
					var child = children.Dequeue();
					if (visited.Contains(child))
						return true;
					visited.Add(child);
					children.Enqueue( graph.GetChildren(child) );
				}
			}

			// any vertex not visited yet must be part of a cycle
			var unvisited = graph.Vertices.Except(visited);
			bool hasCycle = unvisited.Any();
			return !hasCycle;
		}

		// see if removing all root-outedges recursively leaves any edges. if it does, there's a cycle
		public static bool ContainsCycle<T,P>(this GraphWithRoots<T, P> graph) where T : class
		{
			var roots = new Queue<T>(graph.RootVertices);
			var removedEdges = new HashSet<Edge<T, P>>();

			System.Func<T, IEnumerable<Edge<T, P>>> remainingOutEdges = vertex => graph.GetOutEdges(vertex).Except(removedEdges);
			System.Func<T, IEnumerable<Edge<T, P>>> remainingInEdges = vertex => graph.GetInEdges(vertex).Except(removedEdges);

			while (roots.Any())
			{
				var vertex = roots.Dequeue();

				foreach (var edge in remainingOutEdges(vertex))
				{
					removedEdges.Add(edge);

					if (!remainingInEdges(edge.Target).Any())
						roots.Enqueue(edge.Target);
				}
			}

			return graph.Edges.Except(removedEdges).Any();
		}
	}
}