#if UNITY_EDITOR

using UnityEditor;
using System.Collections.Generic;

using TOBI.Core;

namespace TOBI.World
{
    [InitializeOnLoad]
    public static class EditorBuildSettingsAutomator
    {
        // This class hooks into the Play button and automatically configures the Editor Build Settings
        // to include all the scenes that are necessary for the project to run. (This is how new areas
        // can automagically be loaded in Play Mode.)

        // Set up delegates.

        static EditorBuildSettingsAutomator()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                GenerateEditorBuildSettings();
        }

        // Configures the build settings.

        private static string INIT_PATH = "Assets/TOBI/World/Scenes/" + Game.INIT_SCENE_NAME + ".unity";
        private static string[] PATHS = new string[] { "Assets/TOBI/World/Scenes/Areas",
                                                       "Assets/TOBI/World/Scenes/Base",
                                                       "Assets/TOBI/World/Scenes/Distinct" };

        [MenuItem("TOBI/Generate Editor Build Settings", false, 12)]
        private static void GenerateEditorBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

            // Put the initialization scene first.

            scenes.Add(new EditorBuildSettingsScene(INIT_PATH, true));

            // Put all other scenes after.

            string[] GUIDs = AssetDatabase.FindAssets("t:Scene", PATHS);

            foreach (string guid in GUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

#endif