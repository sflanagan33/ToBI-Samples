using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using TOBI.Map;
using TOBI.World;

namespace TOBI.Procedural
{
    [SelectionBase]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ProceduralPolygon : MonoBehaviour, IMappable, ILODable
    {
        private const float VERTEX_DISTANCE_HIGHPOLY_COLLISION = 0.75f;
        private const float VERTEX_DISTANCE_LOWPOLY = 2.5f; // used to be 1.5f
        private const float VERTEX_DISTANCE_MAP = 2.5f;
        private static readonly Point[] DEFAULT_SHAPE = { new Point(+4, -4), new Point(-4, -4), new Point(-4, +4), new Point(+4, +4) };

        public ProceduralPolygonStyle style;
        public List<Point> points = new List<Point>(DEFAULT_SHAPE);
        public bool hasTop = true;
        public bool hasBottom = false;
        public float radius = 1;
        public float height = 4;
        
        private MeshFilter mf;
        public MeshFilter meshFilter
        {
            get
            {
                if (mf == null)
                    mf = GetComponent<MeshFilter>();
                return mf;
            }
        }
        
        private MeshRenderer mr;
        public MeshRenderer meshRenderer
        {
            get
            {
                if (mr == null)
                    mr = GetComponent<MeshRenderer>();
                return mr;
            }
        }

        private bool hasGeneratedOnce = false;
        public string generationMessage = "";

        [SerializeField] private Mesh serializedHighpolyMesh;
        [SerializeField] private List<Mesh> serializedCollisionMeshes;
        [SerializeField] private List<PhysicMaterial> serializedCollisionMaterials;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                // When this polygon starts in play mode, check to see if there's a serialized mesh set we can use

                if (serializedHighpolyMesh)
                {
                    meshFilter.sharedMesh = serializedHighpolyMesh;

                    int collisionIndex = 0;

                    foreach (MeshCollider mc in GetComponents<MeshCollider>())
                    {
                        mc.sharedMesh = serializedCollisionMeshes[collisionIndex];
                        mc.sharedMaterial = serializedCollisionMaterials[collisionIndex];
                        collisionIndex++;
                    }

                    if (collisionIndex != serializedCollisionMeshes.Count)
                        Debug.LogError("Serialized collision mesh count did NOT match mesh collider count", this);
                }

                else
                    Regenerate();
            }

            // Always regenerate in edit mode

            else
                Regenerate();
        }

        // ====================================================================================================================

        public void SerializeMeshes(Mesh highpolyMesh, List<Mesh> collisionMeshes, List<PhysicMaterial> collisionMaterials)
        {
            serializedHighpolyMesh = highpolyMesh;
            serializedCollisionMeshes = collisionMeshes;
            serializedCollisionMaterials = collisionMaterials;
        }

        // ====================================================================================================================

        // Resets the points of this ProceduralPolygon to the default square shape.

        public void ResetShape()
        {
            points = new List<Point>(DEFAULT_SHAPE);
            Regenerate();
        }

        // Resets the materials of this ProceduralPolygon's mesh renderer to the ones specified in its style, if it has one.

        public void ResetMaterials()
        {
            if (style != null)
                meshRenderer.sharedMaterials = style.defaultMaterials;
        }

        // Centers the pivot of this ProceduralPolygon, by placing its transform at the average of its points.

        public void CenterPivot()
        {
            int n = points.Count;
            Point average = new Point();

            for (int i = 0; i < n; i++)
                average += points[i];
            average /= n;

            for (int i = 0; i < n; i++)
                points[i] -= average;
            Regenerate();

            transform.position += transform.TransformVector(average);
        }

        // ====================================================================================================================

        // Regenerates this ProceduralPolygon, calculating its highpoly mesh and collision meshes and applying them
        // to the GameObject. Called whenever the polygon is edited and once on Awake when the polygon loads ingame.

        public void Regenerate()
        {
            // Cleanup

            if (hasGeneratedOnce && meshFilter.sharedMesh != null)
                DestroyImmediate(meshFilter.sharedMesh);

            foreach (MeshCollider mc in GetComponents<MeshCollider>())
            {
                if (hasGeneratedOnce && mc.sharedMesh != null)
                    DestroyImmediate(mc.sharedMesh);
                DestroyImmediate(mc);
            }

            hasGeneratedOnce = true;
            generationMessage = "";

            // Generation

            if (style != null)
            {
                Polygon p = new Polygon(points, false);
                PolygonRounding.Result rr = PolygonRounding.Round(p, radius, VERTEX_DISTANCE_HIGHPOLY_COLLISION);

                if (rr.success)
                {
                    gameObject.tag = style.tag;

                    meshFilter.sharedMesh = style.GenerateHighpolyMesh(rr.polygon, hasTop, hasBottom, height);

                    if (meshFilter.sharedMesh == null)
                        generationMessage = "The given radius is too large for this polygon.";

                    List<Mesh> collisionMeshes = style.GenerateCollisionMeshes(rr.polygon, hasTop, hasBottom, height);
                    
                    if (collisionMeshes != null)
                    {
                        for (int i = 0; i < collisionMeshes.Count; i++)
                        {
                            MeshCollider mc = gameObject.AddComponent<MeshCollider>();
                            mc.cookingOptions &= ~MeshColliderCookingOptions.EnableMeshCleaning;
                            mc.sharedMaterial = style.defaultPhysicMaterials[i];
                            mc.sharedMesh = collisionMeshes[i];

                            if (style.isTrigger)
                            {
                                mc.convex = true;
                                mc.isTrigger = true;
                            }
                        }
                    }
                }

                else
                    generationMessage = rr.failureReason;
            }

            else
                generationMessage = "This polygon has no style (or grace.)";
        }

        // ====================================================================================================================

        // Flips this polygon along a specified axis.

        public void Flip(char AXIS)
        {
            CenterPivot();

            int n = points.Count;

            for (int i = 0; i < n; i++)
            {
                Point flippedPoint = points[i];
                if (AXIS == 'X')
                {
                    flippedPoint = new Point(-points[i].x, points[i].y);
                }
                else if (AXIS == 'Z')
                {
                    flippedPoint = new Point(points[i].x, -points[i].y);
                }
                points[i] = flippedPoint;
            }

            points.Reverse();

            Regenerate();
        }

        // ====================================================================================================================

        MapResult IMappable.GetMap()
        {
            if (!style.showOnMap)
                return new MapResult();

            Polygon p = new Polygon(points, false);
            PolygonRounding.Result rr = PolygonRounding.Round(p, radius, VERTEX_DISTANCE_MAP);

            List<Vector3> verts = new List<Vector3>();
            List<PolygonTriangulation.Result> result = PolygonTriangulation.Triangulate(rr.polygon.points);

            foreach (PolygonTriangulation.Result triangle in result)
            {
                verts.Add(new Vector3(triangle.a.x, height, triangle.a.y));
                verts.Add(new Vector3(triangle.b.x, height, triangle.b.y));
                verts.Add(new Vector3(triangle.c.x, height, triangle.c.y));
            }
            
            Mesh mesh = new Mesh();
            mesh.hideFlags = HideFlags.HideAndDontSave;
            mesh.SetVertices(verts);
            mesh.SetTriangles(Enumerable.Range(0, result.Count * 3).ToArray(), 0);
            mesh.SetColors(Enumerable.Repeat(style.mapColor, result.Count * 3).ToList());

            return new MapResult(transform.localToWorldMatrix, mesh);
        }

        // ====================================================================================================================

        LODResult ILODable.GetLOD()
        {
            Polygon p = new Polygon(points, false);
            PolygonRounding.Result rr = PolygonRounding.Round(p, radius, VERTEX_DISTANCE_LOWPOLY);

            Mesh mesh = style.GenerateLowpolyMesh(rr.polygon, hasTop, hasBottom, height);

            return new LODResult(transform.localToWorldMatrix, mesh, meshRenderer.sharedMaterials);
        }
    }
}