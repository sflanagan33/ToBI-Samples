#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace TOBI.Procedural
{
    public class ProceduralPolygonSerializer : MonoBehaviour
    {
        private const string SERIALIZATION_PATH = "Assets/TOBI/Resources/Polygon/";

        [MenuItem("TOBI/Serialize All Open Polygons", false, 12)]
        public static void SerializePolygons()
        {
            ProceduralPolygon[] polygons = FindObjectsOfType<ProceduralPolygon>();
            Undo.RecordObjects(polygons, "Serialize All Open Polygons");

            foreach (ProceduralPolygon p in polygons)
            {
                string polygonHash = p.gameObject.scene.name.Replace(' ', '_') + "_" + p.transform.position.GetHashCode() + "_" + p.points.GetHashCode();

                // ================================================================================================================================== HIGHPOLY
                
                Mesh highpolyMesh = Instantiate(p.meshFilter.sharedMesh);
                
                string highpolyFilename = SERIALIZATION_PATH + polygonHash + "_H" + ".asset";

                if (AssetDatabase.LoadAssetAtPath(highpolyFilename, typeof(Mesh)) != null)
                    Debug.LogError("HASH COLLISION FOR HIGHPOLY!", p);
                AssetDatabase.CreateAsset(highpolyMesh, highpolyFilename);

                // ================================================================================================================================== COLLISION
                
                List<Mesh> collisionMeshes = new List<Mesh>();
                List<PhysicMaterial> collisionMaterials = new List<PhysicMaterial>();
                int collisionIndex = 0;

                foreach (MeshCollider mc in p.GetComponents<MeshCollider>())
                {
                    Mesh collisionMesh = Instantiate(mc.sharedMesh);

                    string collisionFilename = SERIALIZATION_PATH + polygonHash + "_C" + collisionIndex + ".asset";

                    if (AssetDatabase.LoadAssetAtPath(collisionFilename, typeof(Mesh)) != null)
                        Debug.LogError("HASH COLLISION FOR COLLISION " + collisionIndex + "!");
                    AssetDatabase.CreateAsset(collisionMesh, collisionFilename);

                    collisionMeshes.Add(collisionMesh);
                    collisionMaterials.Add(mc.sharedMaterial);
                    collisionIndex++;
                }

                // Point the polygon at its serialized mesh set

                p.SerializeMeshes(highpolyMesh, collisionMeshes, collisionMaterials);
                EditorUtility.SetDirty(p);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("TOBI/Deserialize All Open Polygons", false, 12)]
        public static void DeserializePolygons()
        {
            ProceduralPolygon[] polygons = FindObjectsOfType<ProceduralPolygon>();
            Undo.RecordObjects(polygons, "Deserialize All Open Polygons");

            foreach (ProceduralPolygon p in polygons)
            {
                p.SerializeMeshes(null, null, null);
                EditorUtility.SetDirty(p);
            }
        }
    }
}

#endif