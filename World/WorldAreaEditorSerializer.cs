#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace TOBI.World
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class WorldAreaEditorSerializer : MonoBehaviour
    {
        private const string SERIALIZATION_PATH = "Assets/TOBI/Resources/World/";

        private Vector3 areaCenter;
        private Vector3 areaSize;

        public void OnEnable()
        {
            EditorSceneManager.sceneSaving += SerializeScene;
        }

        public void OnDisable()
        {
            EditorSceneManager.sceneSaving -= SerializeScene;
        }

        public void OnValidate()
        {
            name = "WorldAreaEditorSerializer (" + gameObject.scene.name + ")";
        }

        public void SerializeScene(Scene scene, string path)
        {
            if (scene == gameObject.scene)
            {
                // Create a single LOD mesh for this scene and save it as an asset

                // FIrst, find all the LODable objects

                List<ILODable> lods = new List<ILODable>();
                foreach (GameObject g in scene.GetRootGameObjects())
                    lods.AddRange(g.GetComponentsInChildren<ILODable>());

                // Now, create an initial cache that maps the materials being used by the LODs
                // to lists of meshes that will be combined

                Dictionary<Material, List<CombineInstance>> cache = new Dictionary<Material, List<CombineInstance>>();

                foreach (ILODable lod in lods)
                {
                    LODResult r = lod.GetLOD();
                    
                    for (int i = 0; i < r.materials.Length; i++)
                    {
                        CombineInstance c = new CombineInstance();
                        c.mesh = r.mesh;
                        c.subMeshIndex = i;
                        c.transform = r.matrix;

                        Material m = r.materials[i];
                        if (!cache.ContainsKey(m))
                            cache.Add(m, new List<CombineInstance>());
                        cache[m].Add(c);
                    }
                }

                // Then, create one combined mesh for every material

                Dictionary<Material, Mesh> firstCombines = new Dictionary<Material, Mesh>();

                foreach (KeyValuePair<Material, List<CombineInstance>> entry in cache)
                {
                    Mesh newMesh = new Mesh();
                    newMesh.CombineMeshes(entry.Value.ToArray());
                    firstCombines.Add(entry.Key, newMesh);
                }

                // Finally, create one combined mesh for everything

                List<CombineInstance> finalCombine = new List<CombineInstance>();

                foreach (KeyValuePair<Material, Mesh> entry in firstCombines)
                    finalCombine.Add(new CombineInstance() { mesh = entry.Value });

                Mesh lodMesh = new Mesh();
                lodMesh.CombineMeshes(finalCombine.ToArray(), false, false);

                // Save the mesh as an asset

                AssetDatabase.CreateAsset(lodMesh, SERIALIZATION_PATH + scene.name + ".asset");

                // Create a world area prefab which renders that mesh and loads this scene when triggered

                GameObject prefab = new GameObject();

                GameObject prefabChild = new GameObject("Internal");
                prefabChild.transform.SetParent(prefab.transform);

                MeshFilter meshFilter = prefabChild.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = lodMesh;

                MeshRenderer meshRenderer = prefabChild.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = firstCombines.Keys.ToArray();
                
                WorldArea area = prefab.AddComponent<WorldArea>();
                area.scenePath = scene.path;
                area.lod = meshRenderer;
                area.center = areaCenter;
                area.size = areaSize;

                // Save the prefab as an asset, replacing it if it already exists

                string prefabPath = SERIALIZATION_PATH + scene.name + ".prefab";
                
                Object existingPrefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);

                if (existingPrefab == null)
                    existingPrefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
                PrefabUtility.ReplacePrefab(prefab, existingPrefab, ReplacePrefabOptions.ReplaceNameBased);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                DestroyImmediate(prefab);
            }
        }

        // Every editor tick, calculate the center and size of this WorldArea, which is based on the
        // axis-aligned bounding box that encapsulates all of the visible mesh renderers in the scene

        private void Update()
        {
            if (Application.isPlaying)
                return;

            List<MeshRenderer> renderers = new List<MeshRenderer>();

            foreach (GameObject g in gameObject.scene.GetRootGameObjects())
                renderers.AddRange(g.GetComponentsInChildren<MeshRenderer>());

            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;

            if (renderers.Count > 0)
            {
                min = renderers[0].bounds.min;
                max = renderers[0].bounds.max;

                for (int i = 1; i < renderers.Count; i++)
                {
                    min = Vector3.Min(min, renderers[i].bounds.min);
                    max = Vector3.Max(max, renderers[i].bounds.max);
                }
            }
            
            areaCenter = (min + max) / 2f;
            areaSize = (max - min);
        }

        // Draw the bounding box itself in the scene

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0.75f, 0);
            Gizmos.DrawWireCube(areaCenter, areaSize + WorldArea.LOAD_BOUNDARY * Vector3.one);
            Gizmos.color = new Color(0, 0.25f, 1);
            Gizmos.DrawWireCube(areaCenter, areaSize + WorldArea.UNLOAD_BOUNDARY * Vector3.one);
        }
    }
    
    [CustomEditor(typeof(WorldAreaEditorSerializer))]
    public class WorldAreaEditorSerializerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            WorldAreaEditorSerializer serializer = (WorldAreaEditorSerializer) target;

            GUILayout.Space(10);
            GUILayout.Label(serializer.gameObject.scene.name);
            GUILayout.Space(10);

            if (GUILayout.Button("Save"))
                EditorSceneManager.SaveScene(serializer.gameObject.scene);

            if (GUILayout.Button("Save and Close"))
            {
                EditorSceneManager.SaveScene(serializer.gameObject.scene);
                EditorSceneManager.CloseScene(serializer.gameObject.scene, true);
            }

            GUILayout.Space(10);
        }
    }
}

#endif