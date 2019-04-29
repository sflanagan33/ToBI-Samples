#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TOBI.World
{
    public class WorldAreaEditorWindow : EditorWindow
    {
        private const string SAVE_PATH = "Assets/TOBI/World/Scenes/Areas/";

        private string newName;
        private string fileName { get { return SAVE_PATH + newName + ".unity"; } }

        [MenuItem("TOBI/New Area", false, 0)]
        private static void Init()
        {
            WorldAreaEditorWindow window = GetWindow<WorldAreaEditorWindow>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 150);
            window.titleContent = new GUIContent("New Area");
            window.Show();
        }

        private void OnGUI()
        {
            for (int i = 0; i < 5; i++)
                EditorGUILayout.Space();

            newName = EditorGUILayout.TextField("Name", newName);

            bool badInput = string.IsNullOrEmpty(newName);
            bool alreadyExists = false;

            if (!badInput)
                alreadyExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(fileName);

            if (alreadyExists)
                EditorGUILayout.HelpBox("An area with this name already exists.", MessageType.Error);

            EditorGUI.BeginDisabledGroup(badInput || alreadyExists);

            if (GUILayout.Button("Create!"))
            {
                CreateArea();
                Close();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void CreateArea()
        {
            Scene priorActive = SceneManager.GetActiveScene();

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(newScene, fileName);

            GameObject g = new GameObject();
            g.AddComponent<WorldAreaEditorSerializer>();
            g.tag = "EditorOnly";
            EditorSceneManager.SaveScene(newScene, fileName);

            SceneManager.SetActiveScene(priorActive);
        }
    }
}

#endif