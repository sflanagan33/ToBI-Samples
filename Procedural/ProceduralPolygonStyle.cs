using UnityEngine;
using System.Collections.Generic;

namespace TOBI.Procedural
{
    [CreateAssetMenu(fileName = "New Style", menuName = "Procedural Polygon Style", order = 1)]
    public class ProceduralPolygonStyle : ScriptableObject
    {
        [Header("Appearance")]
        public ProceduralPolygonSilhouette highpolySilhouette;
        public ProceduralPolygonSilhouette lowpolySilhouette;
        public Material[] defaultMaterials;
        public int topMatIndex;
        public int bottomMatIndex;
        public float textureSize;
        public bool lightmapStatic = true;

        [Space(20)]
        [Header("Collision")]
        public ProceduralPolygonSilhouette collisionSilhouette;
        public PhysicMaterial[] defaultPhysicMaterials;
        public int topPhysicMatIndex;
        public int bottomPhysicMatIndex;
        public bool isTrigger;
        public string tag = "ClimbableSurface";

        [Space(20)]
        [Header("Map")]
        public bool showOnMap = true;
        public Color mapColor = Color.magenta;

        public Mesh GenerateHighpolyMesh(Polygon polygon, bool hasTop, bool hasBottom, float height)
        {
            return Lathe(highpolySilhouette, polygon, hasTop, hasBottom, height, textureSize);
        }

        public Mesh GenerateLowpolyMesh(Polygon polygon, bool hasTop, bool hasBottom, float height)
        {
            return Lathe(lowpolySilhouette, polygon, hasTop, hasBottom, height, textureSize);
        }

        public List<Mesh> GenerateCollisionMeshes(Polygon polygon, bool hasTop, bool hasBottom, float height)
        {
            return LatheCollision(collisionSilhouette, polygon, hasTop, hasBottom, height);
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

        private static List<float> lengths = new List<float>(CAPACITY);
        private static List<int> bottomIndices = new List<int>(CAPACITY);
        private static List<int> topIndices = new List<int>(CAPACITY);
        private static List<Point> bottomPoints = new List<Point>(CAPACITY);
        private static List<Point> topPoints = new List<Point>(CAPACITY);

        private Mesh Lathe(ProceduralPolygonSilhouette silhouette, Polygon polygon, bool hasTop, bool hasBottom, float height, float textureSize)
        {
            verts.Clear();
            uvs.Clear();
            foreach (List<int> triList in triLists)
                triList.Clear();

            lengths.Clear();
            bottomIndices.Clear();
            topIndices.Clear();
            bottomPoints.Clear();
            topPoints.Clear();
            
            // ======================================================================================== SIDES

            int t = 0;
            int pointsInPolygon = polygon.points.Count + 1;
            
            lengths.Add(0);
            Point pPrev = polygon.points[0];
            float perimeter = 0;

            for (int i = 1; i < pointsInPolygon; i++)
            {
                Point p = polygon.points[i % polygon.points.Count];
                float x = p.x - pPrev.x;
                float y = p.y - pPrev.y;
                pPrev = p;

                perimeter += Mathf.Sqrt(x * x + y * y);
                lengths.Add(perimeter);
            }

            for (int i = 0; i < silhouette.strips.Length; i++)
            {
                ProceduralPolygonStrip strip = silhouette.strips[i];
                int pointsInStrip = strip.points.Length;

                float tSize = (strip.overrideTextureSize > 0) ? strip.overrideTextureSize : textureSize;
                float uScale = -Mathf.Max(1, Mathf.RoundToInt(perimeter / tSize)) / perimeter;

                float stripDistance = 0;

                float prevDistance = 0;
                float prevHeight = 0;

                for (int j = 0; j < pointsInStrip; j++)
                {
                    ProceduralPolygonStripPoint point = strip.points[j];
                    float pointHeight = height * ((int) point.anchor) + point.height;

                    if (j > 0)
                    {
                        float d = point.distance - prevDistance;
                        float h = pointHeight - prevHeight;
                        stripDistance += Mathf.Sqrt(d * d + h * h);
                    }

                    prevDistance = point.distance;
                    prevHeight = pointHeight;

                    float v = 0;
                    if (strip.vBehavior == ProceduralPolygonStripVBehavior.ByHeight)
                        v = pointHeight / tSize;
                    else
                        v = stripDistance / tSize;

                    for (int k = 0; k < pointsInPolygon; k++)
                    {
                        int kp = k % (pointsInPolygon - 1);
                        
                        Point p = polygon.points[kp];
                        Vector2 n = polygon.normals[kp];
                        Vector3 pos = new Vector3(p.x + n.x * point.distance,
                                                  pointHeight,
                                                  p.y + n.y * point.distance);

                        float u = lengths[k] * uScale + strip.uShift;

                        verts.Add(pos);
                        uvs.Add(new Vector2(u, v));

                        if (j < pointsInStrip - 1 && k < pointsInPolygon - 1)
                        {
                            int t1 = t + (j * pointsInPolygon) + (k);
                            int t2 = t + (j * pointsInPolygon) + (k + 1);
                            int t3 = t + ((j + 1) * pointsInPolygon) + (k);
                            int t4 = t + ((j + 1) * pointsInPolygon) + (k + 1);

                            if (i == 0 && j == 0)
                                bottomIndices.Add(t1);
                            if (i == silhouette.strips.Length - 1 && j == pointsInStrip - 2)
                                topIndices.Add(t3);

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

                t += pointsInStrip * pointsInPolygon;
            }

            // ======================================================================================== TOP

            if (hasTop)
            {
                foreach (int i in topIndices)
                    topPoints.Add(verts[i]);

                List<PolygonTriangulation.Result> result = PolygonTriangulation.Triangulate(topPoints);
                if (result == null)
                    return null;

                float h = verts[topIndices[0]].y;

                foreach (PolygonTriangulation.Result triangle in result)
                {
                    verts.Add(new Vector3(triangle.a.x, h, triangle.a.y));
                    verts.Add(new Vector3(triangle.b.x, h, triangle.b.y));
                    verts.Add(new Vector3(triangle.c.x, h, triangle.c.y));

                    uvs.Add(triangle.a / textureSize);
                    uvs.Add(triangle.b / textureSize);
                    uvs.Add(triangle.c / textureSize);

                    List<int> l = triLists[topMatIndex];
                    l.Add(t++);
                    l.Add(t++);
                    l.Add(t++);
                }
            }

            // ======================================================================================== BOTTOM

            if (hasBottom)
            {
                foreach (int i in bottomIndices)
                    bottomPoints.Add(verts[i]);

                List<PolygonTriangulation.Result> result = PolygonTriangulation.Triangulate(bottomPoints);
                if (result == null)
                    return null;

                float h = verts[bottomIndices[0]].y;

                foreach (PolygonTriangulation.Result triangle in result)
                {
                    verts.Add(new Vector3(triangle.a.x, h, triangle.a.y));
                    verts.Add(new Vector3(triangle.c.x, h, triangle.c.y));
                    verts.Add(new Vector3(triangle.b.x, h, triangle.b.y));

                    uvs.Add(triangle.a / textureSize);
                    uvs.Add(triangle.c / textureSize);
                    uvs.Add(triangle.b / textureSize);

                    List<int> l = triLists[bottomMatIndex];
                    l.Add(t++);
                    l.Add(t++);
                    l.Add(t++);
                }
            }

            // ======================================================================================== FINAL

            Mesh mesh = new Mesh();
            mesh.name = "Generated Mesh";
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

        private List<Mesh> LatheCollision(ProceduralPolygonSilhouette silhouette, Polygon polygon, bool hasTop, bool hasBottom, float height)
        {
            verts.Clear();
            foreach (List<int> triList in triLists)
                triList.Clear();
            
            bottomIndices.Clear();
            topIndices.Clear();
            bottomPoints.Clear();
            topPoints.Clear();

            collisionResult.Clear();

            // ======================================================================================== SIDES

            int t = 0;
            int pointsInPolygon = polygon.points.Count + 1;

            for (int i = 0; i < silhouette.strips.Length; i++)
            {
                ProceduralPolygonStrip strip = silhouette.strips[i];
                int pointsInStrip = strip.points.Length;
                
                for (int j = 0; j < pointsInStrip; j++)
                {
                    ProceduralPolygonStripPoint point = strip.points[j];
                    float pointHeight = height * ((int) point.anchor) + point.height;

                    for (int k = 0; k < pointsInPolygon; k++)
                    {
                        int kp = k % (pointsInPolygon - 1);

                        Point p = polygon.points[kp];
                        Vector2 n = polygon.normals[kp];
                        Vector3 pos = new Vector3(p.x + n.x * point.distance,
                                                  pointHeight,
                                                  p.y + n.y * point.distance);

                        verts.Add(pos);

                        if (j < pointsInStrip - 1 && k < pointsInPolygon - 1)
                        {
                            int t1 = t + (j * pointsInPolygon) + (k);
                            int t2 = t + (j * pointsInPolygon) + (k + 1);
                            int t3 = t + ((j + 1) * pointsInPolygon) + (k);
                            int t4 = t + ((j + 1) * pointsInPolygon) + (k + 1);

                            if (i == 0 && j == 0)
                                bottomIndices.Add(t1);

                            if (i == silhouette.strips.Length - 1 && j == pointsInStrip - 2)
                                topIndices.Add(t3);

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

                t += pointsInStrip * pointsInPolygon;
            }

            // ======================================================================================== TOP

            if (hasTop)
            {
                foreach (int i in topIndices)
                    topPoints.Add(verts[i]);

                List<PolygonTriangulation.Result> result = PolygonTriangulation.Triangulate(topPoints);
                if (result == null)
                    return null;

                float h = verts[topIndices[0]].y;

                foreach (PolygonTriangulation.Result triangle in result)
                {
                    verts.Add(new Vector3(triangle.a.x, h, triangle.a.y));
                    verts.Add(new Vector3(triangle.b.x, h, triangle.b.y));
                    verts.Add(new Vector3(triangle.c.x, h, triangle.c.y));

                    List<int> l = triLists[topPhysicMatIndex];
                    l.Add(t++);
                    l.Add(t++);
                    l.Add(t++);
                }
            }

            // ======================================================================================== BOTTOM

            if (hasBottom)
            {
                foreach (int i in bottomIndices)
                    bottomPoints.Add(verts[i]);

                List<PolygonTriangulation.Result> result = PolygonTriangulation.Triangulate(bottomPoints);
                if (result == null)
                    return null;

                float h = verts[bottomIndices[0]].y;

                foreach (PolygonTriangulation.Result triangle in result)
                {
                    verts.Add(new Vector3(triangle.a.x, h, triangle.a.y));
                    verts.Add(new Vector3(triangle.c.x, h, triangle.c.y));
                    verts.Add(new Vector3(triangle.b.x, h, triangle.b.y));

                    List<int> l = triLists[bottomPhysicMatIndex];
                    l.Add(t++);
                    l.Add(t++);
                    l.Add(t++);
                }
            }

            // ======================================================================================== FINAL
            
            // Find the trilists that were actually used and make one mesh for each

            for (int i = 0; i < triLists.Count; i++)
            {
                List<int> triList = triLists[i];
                if (triList.Count > 0)
                {
                    Mesh mesh = new Mesh();
                    mesh.name = "Generated Mesh";
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