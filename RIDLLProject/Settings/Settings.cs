using System.IO;
using UnityEngine;
using UnityEditor;

namespace RelationsInspector
{
    internal class Settings
    {
        static RelationsInspectorSettings storage = Util.LoadOrCreate( ProjectSettings.SettingsPath, CreateDefaultStorage );
        internal static RelationsInspectorSettings Instance
        {
            get { return storage; }
        }

        static RelationsInspectorSettings CreateDefaultStorage()
        {
            var storage = ScriptableObject.CreateInstance<RelationsInspectorSettings>();

            // initialize with default values
            storage.maxGraphNodes = 40;
            storage.treeRootLocation = TreeRootLocation.Left;
            storage.showMinimap = true;
            storage.minimapLocation = MinimapLocation.TopLeft;
            storage.layoutTweenParameters = new LayoutTweenParameters();
            storage.logToConsole = true;
            return storage;
        }
    }
}
