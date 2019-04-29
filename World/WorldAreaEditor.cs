#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Linq;

namespace TOBI.World
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WorldArea))]
    public class WorldAreaEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            WorldArea[] areas = Array.ConvertAll(targets, a => (WorldArea) a);

            GUILayout.Space(10);

            if (areas.Length == 1)
                GUILayout.Label(areas[0].name, EditorStyles.boldLabel);
            else
                GUILayout.Label("(multiple areas)", EditorStyles.boldLabel);

            GUILayout.Space(10);

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("These controls are unavailable in play mode.", MessageType.Warning);
                return;
            }

            // Figure out if all and any the inspected areas are currently open

            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            bool allOpen = areas.All(a => scenes.Any(s => s.path == a.scenePath && s.isLoaded));
            bool anyOpen = areas.Any(a => scenes.Any(s => s.path == a.scenePath && s.isLoaded));

            // Show open / close buttons

            EditorGUI.BeginDisabledGroup(allOpen);

            if (GUILayout.Button("Open"))
            {
                foreach (WorldArea a in areas)
                    EditorSceneManager.OpenScene(a.scenePath, OpenSceneMode.Additive);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!anyOpen);

            if (GUILayout.Button("Save and Close"))
            {
                foreach (WorldArea a in areas)
                {
                    Scene s = SceneManager.GetSceneByPath(a.scenePath);
                    EditorSceneManager.SaveScene(s);
                    EditorSceneManager.CloseScene(s, true);
                }
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
        }
    }
}

#endif