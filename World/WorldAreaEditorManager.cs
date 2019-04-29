#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TOBI.World
{
    [ExecuteInEditMode]
    public class WorldAreaEditorManager : MonoBehaviour
    {
        private const string LOAD_PATH = "World/";
        private const string SCENE_KEY = "WorldAreaEditorManagerScenes";
        private const char SCENE_DELIMITER = '|';

        // Set up delegates.

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
            EditorSceneManager.sceneOpened += SceneOpened;
            EditorSceneManager.sceneClosed += SceneClosed;
            EditorSceneManager.sceneSaving += SceneSaving;
            EditorSceneManager.sceneSaved += SceneSaved;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            EditorSceneManager.sceneOpened -= SceneOpened;
            EditorSceneManager.sceneClosed -= SceneClosed;
            EditorSceneManager.sceneSaving -= SceneSaving;
            EditorSceneManager.sceneSaved -= SceneSaved;
        }

        // When the Unity editor loads, instantiate all the areas. (This is because the normal SceneOpened
        // callback does not get called when the scene is the default one loaded by the editor.) This also
        // fires at some spuriously documented times, such as after scripts are recompiled.
        
        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.update += OnLoadCallback;
        }

        static void OnLoadCallback()
        {
            EditorApplication.update -= OnLoadCallback;
            WorldAreaEditorManager w = FindObjectOfType<WorldAreaEditorManager>();

            if (w != null)
            {
                w.DestroyAllAreas();
                w.InstantiateAllAreas();
            }
        }

        // When edit mode is exited or entered, we should destroy or create all the area prefabs respectively.
        // We should also close (and later reopen) any area scenes that were open when the Play button was pressed
        // so that they can be properly streamed during gameplay by the normal WorldAreaManager.

        private void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                string scenesToReopen = "";

                foreach (WorldArea a in GetComponentsInChildren<WorldArea>(true))
                {
                    Scene s = SceneManager.GetSceneByPath(a.scenePath);

                    if (s.IsValid())
                    {
                        scenesToReopen += a.scenePath + SCENE_DELIMITER;
                        EditorSceneManager.SaveScene(s);
                        EditorSceneManager.CloseScene(s, true);
                    }
                }

                scenesToReopen = scenesToReopen.TrimEnd(SCENE_DELIMITER);
                if (!string.IsNullOrEmpty(scenesToReopen))
                    EditorPrefs.SetString(SCENE_KEY, scenesToReopen);

                DestroyAllAreas();
            }

            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (EditorPrefs.HasKey(SCENE_KEY))
                {
                    string[] scenesToReopen = EditorPrefs.GetString(SCENE_KEY).Split(SCENE_DELIMITER);
                    foreach (string scenePath in scenesToReopen)
                        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                    EditorPrefs.DeleteKey(SCENE_KEY);
                }

                InstantiateAllAreas();
            }
        }

        // When a scene is opened in the editor, we want to hide its area prefab in the hierarchy (if it exists)
        // If it's the base scene (i.e. this manager just got created), we need to spawn all of the area prefabs

        private void SceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene == gameObject.scene)
                InstantiateAllAreas();

            else
            {
                WorldArea a = FindInstantiatedAreaForScene(scene);
                if (a != null)
                    SetAreaActive(a, false);
            }
        }

        // When a scene is closed in the editor, we want to show its area prefab in the hierarchy (if it exists)

        private void SceneClosed(Scene scene)
        {
            WorldArea a = FindInstantiatedAreaForScene(scene);
            if (a != null)
                SetAreaActive(a, true);
        }

        // Right before the base scene is saved in the editor, delete all spawned area prefabs so that they aren't saved to disk as changes.
        // This is to make the scene editing workflow much smoother wrt Git, since (e.g.) adding a new area scene won't register as a "change"
        // in the base scene (which would often lead to merge conflicts.)

        private void SceneSaving(Scene scene, string path)
        {
            if (scene == gameObject.scene)
                DestroyAllAreas();
        }

        // Right after a scene is saved, we do one of two things. If it's the base scene, we need to respawn all of the area prefabs since we
        // just deleted them in the above method. If it's not, it could be a new area scene that needs its area prefab instantiated.

        private void SceneSaved(Scene scene)
        {
            if (scene == gameObject.scene)
                InstantiateAllAreas();

            else
            {
                WorldArea a = FindInstantiatedAreaForScene(scene);

                if (a == null)
                    InstantiateArea(scene.name);
                else
                    SetAreaActive(a);
            }
        }

        // Allow for manual refreshing of the area prefabs, too.

        public void RefreshList()
        {
            DestroyAllAreas();
            InstantiateAllAreas();
        }

        // ==================================================================================================================== Helpers

        // Instantiates a single area prefab based on the name of the scene it represents.
        
        private void InstantiateArea(string name)
        {
            GameObject loadedPrefab = Resources.Load<GameObject>(LOAD_PATH + name);

            if (loadedPrefab != null)
            {
                GameObject areaPrefab = (GameObject) PrefabUtility.InstantiatePrefab(loadedPrefab);
                areaPrefab.transform.SetParent(transform);
                SetAreaActive(areaPrefab.GetComponent<WorldArea>());
            }
        }

        // Instantiates all of the area prefabs in the World folder (see LOAD_PATH).

        private void InstantiateAllAreas()
        {
            GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(LOAD_PATH);

            foreach (GameObject loadedPrefab in loadedPrefabs)
            {
                GameObject areaPrefab = (GameObject) PrefabUtility.InstantiatePrefab(loadedPrefab);
                areaPrefab.transform.SetParent(transform);
                SetAreaActive(areaPrefab.GetComponent<WorldArea>());
            }
        }

        // Sets the given area to be active or not based on whether its scene is loaded, or to the given active state
        // if one is provided. This is basically just to hide the attached LOD meshes when they're not needed.

        private void SetAreaActive(WorldArea area, bool? active = null)
        {
            if (active != null)
                area.gameObject.SetActive((bool) active);

            else
            {
                SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
                bool open = scenes.Any(s => s.path == area.scenePath && s.isLoaded);
                area.gameObject.SetActive(!open);
            }
        }

        // Destroys all the instantiated area prefabs.

        private void DestroyAllAreas()
        {
            foreach (WorldArea area in GetComponentsInChildren<WorldArea>(true))
                DestroyImmediate(area.gameObject);
        }

        // Finds the instantiated area prefab associated with the given scene.

        private WorldArea FindInstantiatedAreaForScene(Scene scene)
        {
            WorldArea[] areas = GetComponentsInChildren<WorldArea>(true);
            return areas.SingleOrDefault(a => a.scenePath == scene.path);
        }
    }

    [CustomEditor(typeof(WorldAreaEditorManager))]
    public class WorldAreaEditorManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            WorldAreaEditorManager manager = (WorldAreaEditorManager) target;

            GUILayout.Space(10);
            GUILayout.Label(manager.gameObject.scene.name, EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("These controls are unavailable in play mode.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Refresh List"))
                manager.RefreshList();

            // Figure out if all areas are currently open and show an "Open All" button

            SceneSetup[] scenes = EditorSceneManager.GetSceneManagerSetup();
            WorldArea[] areas = manager.GetComponentsInChildren<WorldArea>(true);

            bool allOpen = areas.All(a => scenes.Any(s => s.path == a.scenePath && s.isLoaded));
            
            EditorGUI.BeginDisabledGroup(allOpen);
            
            if (GUILayout.Button("Open All"))
            {
                foreach (WorldArea a in areas)
                    EditorSceneManager.OpenScene(a.scenePath, OpenSceneMode.Additive);
            }

            EditorGUI.EndDisabledGroup();

            // Figure out if any areas are currently open and show a "Save and Close All" button

            WorldArea[] areasOpen = areas.Where(a => scenes.Any(s => s.path == a.scenePath && s.isLoaded)).ToArray();

            EditorGUI.BeginDisabledGroup(areasOpen.Length == 0);

            if (GUILayout.Button("Save and Close All"))
            {
                foreach (WorldArea a in areasOpen)
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