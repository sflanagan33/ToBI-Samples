using UnityEngine;
using System.Collections.Generic;

namespace TOBI.Procedural
{
    [CreateAssetMenu(fileName = "New Style", menuName = "Procedural Spline Style", order = 1)]
    public class ProceduralSplineStyle : ScriptableObject
    {
        [Header("Appearance")]
        public ProceduralSplineSilhouette highpolySilhouette;
        public ProceduralSplineSilhouette lowpolySilhouette;
        public Material[] defaultMaterials;
        public float textureSize = 1;

        [Space(20)]
        [Header("Collision")]
        public ProceduralSplineSilhouette collisionSilhouette;
        public PhysicMaterial[] defaultPhysicMaterials;

        [Space(20)]
        [Header("Grind Behavior")]
        public Vector3 grindOffset;

        public Mesh GenerateHighpolyMesh(Spline spline)
        {
            return Lathe(highpolySilhouette, spline, textureSize);
        }

        public Mesh GenerateLowpolyMesh(Spline spline)
        {
            return Lathe(lowpolySilhouette, spline, textureSize);
        }

        public List<Mesh> GenerateCollisionMeshes(Spline spline)
        {
            return LatheCollision(collisionSilhouette, spline);
        }

        // ====================================================================================================================

        // Cached local variables to avoid GC

        private static int CAPACITY = 512;
        private static int TRI_CAPACITY = 1024;

        private static List<Vector3> verts = new List<Vector3>(CAPACITY);
        private static List<Vector2> uvs = new List<Vector2>(CAPACITY);
        private static List<List<int>> triLists = new List<List<int>>
        {
            new List<int>(TRI_CAPACITY), new List<int>(TRI_CAPACITY), new List<int>(TRI_CAPACITY), new List<int>(TRI_CAPACITY),
            new List<int>(TRI_CAPACITY), new List<int>(TRI_CAPACITY), new List<int>(TRI_CAPACITY), new List<int>(TRI_CAPACITY)
        };

        private Mesh Lathe(ProceduralSplineSilhouette silhouette, Spline spline, float textureSize)
        {
            verts.Clear();
            uvs.Clear();
            foreach (List<int> triList in triLists)
                triList.Clear();

            // ======================================================================================== SIDES

            int t = 0;
            int pointsInSpline = spline.count;

            float totalLength = spline.lengths[spline.count - 1];
            float uScale = -Mathf.Max(1, Mathf.RoundToInt(totalLength / textureSize)) / totalLength;

            for (int i = 0; i < silhouette.strips.Length; i++)
            {
                ProceduralSplineStrip strip = silhouette.strips[i];
                int pointsInStrip = strip.points.Length;

                for (int j = 0; j < pointsInStrip; j++)
                {
                    ProceduralSplineStripPoint point = strip.points[j];

                    for (int k = 0; k < pointsInSpline; k++)
                    {
                        Vector3 pos = spline.rotations[k] * new Vector3(point.x, point.y) + spline.vertices[k];
                        
                        verts.Add(pos);
                        uvs.Add(new Vector2(spline.lengths[k] * uScale, point.v));

                        if (j < pointsInStrip - 1 && k < pointsInSpline - 1)
                        {
                            int t1 = t + (j * pointsInSpline) + (k);
                            int t2 = t + (j * pointsInSpline) + (k + 1);
                            int t3 = t + ((j + 1) * pointsInSpline) + (k);
                            int t4 = t + ((j + 1) * pointsInSpline) + (k + 1);

                            List<int> l = triLists[strip.materialIndex];
                            l.Add(t1);
                            l.Add(t2);
                            l.Add(t3);
                            l.Add(t3);
                            l.Add(t2);
                            l.Add(t4);
                        }
                    }
                }

                t += pointsInStrip * pointsInSpline;
            }

            // ======================================================================================== FINAL

            Mesh mesh = new Mesh();
            mesh.name = "Generated Mesh (" + silhouette.name + ")";
            mesh.hideFlags = HideFlags.HideAndDontSave;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);

            // Find the trilists that were actually used and set them to the submeshes

            for (int i = 0; i < triLists.Count; i++)
            {
                List<int> triList = triLists[i];
                if (triList.Count > 0)
                {
                    mesh.subMeshCount++;
                    mesh.SetTriangles(triList, i);
                }
            }

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            return mesh;
        }

        // ====================================================================================================================

        // Cached local variables to avoid GC

        private static List<Mesh> collisionResult = new List<Mesh>(8);
        
        private List<Mesh> LatheCollision(ProceduralSplineSilhouette silhouette, Spline spline)
        {
            verts.Clear();
            foreach (List<int> triList in triLists)
                triList.Clear();

            collisionResult.Clear();

            // ======================================================================================== SIDES

            int t = 0;
            int pointsInSpline = spline.count;

            for (int i = 0; i < silhouette.strips.Length; i++)
            {
                ProceduralSplineStrip strip = silhouette.strips[i];
                int pointsInStrip = strip.points.Length;

                for (int j = 0; j < pointsInStrip; j++)
                {
                    ProceduralSplineStripPoint point = strip.points[j];

                    for (int k = 0; k < pointsInSpline; k++)
                    {
                        Vector3 pos = spline.rotations[k] * new Vector3(point.x, point.y) + spline.vertices[k];

                        verts.Add(pos);

                        if (j < pointsInStrip - 1 && k < pointsInSpline - 1)
                        {
                            int t1 = t + (j * pointsInSpline) + (k);
                            int t2 = t + (j * pointsInSpline) + (k + 1);
                            int t3 = t + ((j + 1) * pointsInSpline) + (k);
                            int t4 = t + ((j + 1) * pointsInSpline) + (k + 1);

                            List<int> l = triLists[strip.materialIndex];
                            l.Add(t1);
                            l.Add(t2);
                            l.Add(t3);
                            l.Add(t3);
                            l.Add(t2);
                            l.Add(t4);
                        }
                    }
                }

                t += pointsInStrip * pointsInSpline;
            }

            // ======================================================================================== FINAL

            // Find the trilists that were actually used and make one mesh for each

            for (int i = 0; i < triLists.Count; i++)
            {
                List<int> triList = triLists[i];
                if (triList.Count > 0)
                {
                    Mesh mesh = new Mesh();
                    mesh.name = "Generated Mesh (" + silhouette.name + " " + i + ")";
                    mesh.hideFlags = HideFlags.HideAndDontSave;
                    mesh.SetVertices(verts);
                    mesh.SetTriangles(triList, 0);
                    collisionResult.Add(mesh);
                }
            }

            return collisionResult;
        }
    }
}