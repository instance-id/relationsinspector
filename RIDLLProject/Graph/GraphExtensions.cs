using UnityEngine;
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
            if ( !graph.Vertices.Contains( vertex ) )
                return Enumerable.Empty<T>();

			return graph.VerticesData[vertex].OutEdges.Get().Select(e => e.Target);
		}

		public static IEnumerable<T> GetParents<T, P>(this Graph<T, P> graph, T vertex) where T : class
		{
			if (!graph.Vertices.Contains(vertex))
				return Enumerable.Empty<T>();

			return graph.VerticesData[vertex].InEdges.Get().Select(e => e.Source);
		}

		public static IEnumerable<Relation<T, P>> GetEdges<T, P>(this Graph<T, P> graph, T vertex) where T : class
		{
			return graph.GetInEdges(vertex).Concat( graph.GetOutEdges(vertex) );
		}

		public static IEnumerable<Relation<T, P>> GetOutEdges<T, P>(this Graph<T, P> graph, T vertex) where T : class
		{
			if (!graph.Vertices.Contains(vertex))
				return Enumerable.Empty<Relation<T, P>>();

			return graph.VerticesData[vertex].OutEdges.Get();
		}

		public static IEnumerable<Relation<T, P>> GetInEdges<T, P>(this Graph<T, P> graph, T vertex) where T : class
		{
			if (!graph.Vertices.Contains(vertex))
				return Enumerable.Empty<Relation<T, P>>();

			return graph.VerticesData[vertex].InEdges.Get();
		}

        public static bool IsSingleTree<T, P>(this Graph<T, P> graph) where T : class
        {
            var roots = graph
                .Vertices
                .Where( v => graph.IsRoot( v ) );

            if (roots.Count() != 1)
                return false;

            T root = roots.Single();
            return graph.IsConnectedTree( root );
        }

        // returns tree if the graph forms a tree with root as root node and no disconnected nodes
        public static bool IsConnectedTree<T,P>(this Graph<T, P> graph, T root) where T : class
		{
            // mark all root children as visited (recursively)
            // if any are visited twice, there is a diamod or cycle
            var visited = new HashSet<T>( new[] { root } );
			var children = new HashSet<T>(graph.GetChildren(root));
			while (children.Any())
			{
				var child = children.First();
                children.Remove(child);

				if (visited.Contains(child))
					return false;
				visited.Add(child);
                children.UnionWith(graph.GetChildren(child) );
			}

			// any vertex not visited yet must be part of a cycle
			var unvisited = graph.Vertices.Except(visited);
			bool hasCycle = unvisited.Any();
			return !hasCycle;
		}
	}
}
