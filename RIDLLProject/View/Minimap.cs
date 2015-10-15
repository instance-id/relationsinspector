using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Linq;
using RelationsInspector.Extensions;
using System.Collections.Generic;

namespace RelationsInspector
{
    public enum MinimapLocation { TopLeft, TopRight, BottomLeft, BottomRight };

    internal class Minimap
	{
        const int verticalOffset = 16;  // the editor window draws this canvas in a layout group. that group adds space at the top

        internal static Rect GetRect(MinimapStyle mmStyle, MinimapLocation location, Rect contextRect)
        {
            int width, height;
            width = height = mmStyle.size;
            int spacing = mmStyle.spacing;
            switch ( location )
            {
                case MinimapLocation.TopLeft:
                default:
                    return new Rect( contextRect.x + spacing, contextRect.y + spacing - verticalOffset, width, height );

                case MinimapLocation.TopRight:
                    return new Rect( contextRect.xMax - spacing - width, contextRect.y + spacing - verticalOffset, width, height );

                case MinimapLocation.BottomLeft:
                    return new Rect(contextRect.x + spacing, contextRect.yMax - spacing - height - verticalOffset, width, height);

                case MinimapLocation.BottomRight:
                    return new Rect(contextRect.xMax - spacing - width, contextRect.yMax - spacing - height - verticalOffset, width, height);
            }
        }

        internal static Vector2 Draw( IEnumerable<Vector2> vertexPositions, Rect drawRect, Rect viewRect, bool showGraphBounds, MinimapStyle style )
        {
            // draw black outline
            EditorGUI.DrawRect( drawRect.AddBorder( 1f ), Color.black );
            // draw background
            EditorGUI.DrawRect( drawRect, style.backgroundColor );

            // fit vertex positions and viewRect into drawRect
            var pointsToFit = vertexPositions.Concat( new[] { viewRect.min, viewRect.max } );
            var mmTransform = ViewUtil.FitPointsIntoRect( pointsToFit, drawRect.Scale(0.9f) );

            // draw view rect
            Rect tViewRect = mmTransform.Apply( viewRect ).Intersection( drawRect );
            Util.DrawRectOutline( tViewRect, style.viewRectColor );

            // draw vertex positions
            foreach(var pos in vertexPositions)
                EditorGUI.DrawRect( Util.CenterRect( mmTransform.Apply( pos ), 2, 2 ), style.vertexMarkerColor );

            // find new center: if there is a mousedown event in the rect, make that position the new view center
            Vector2 newCenter = viewRect.center;
            var ev = Event.current;
            switch ( ev.type )
            {
                case EventType.mouseDown:
                    if ( drawRect.Contains( ev.mousePosition ) )
                    {
                        newCenter = mmTransform.Revert( ev.mousePosition );
                        ev.Use();
                    }
                    break;
            }

            return newCenter;
        }
	}
}
