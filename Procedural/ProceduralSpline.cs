using UnityEngine;
using System.Collections.Generic;

using TOBI.World;

namespace TOBI.Procedural
{
    [SelectionBase]
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ProceduralSpline : MonoBehaviour, ILODable
    {
        private static readonly SplineNode[] DEFAULT_LINE = { new SplineNode(Vector3.back * 10f, Quaternion.identity, 10),
                                                              new SplineNode(Vector3.forward * 10f, Quaternion.identity, 10) };

        public ProceduralSplineStyle style;
        public List<SplineNode> nodes = new List<SplineNode>(DEFAULT_LINE);
        public bool closed = false;
        public int precision = 10;
        public SplineOrientation orientation;
        
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
        
        private Spline s;
        public Spline spline
        {
            get
            {
                if (s == null)
                    RegenerateSpline();
                return s;
            }
        }

        private bool hasGeneratedOnce = false;
        
        private void Awake()
        {
            Regenerate();
        }

        // ==================================================================================================================== TOOLS

        // Resets the points of this ProceduralSpline to the default line.

        public void ResetShape()
        {
            nodes = new List<SplineNode>(DEFAULT_LINE);
            closed = false;
            precision = 10;
            Regenerate();
        }

        // Resets the materials of this ProceduralSpline's mesh renderer to the ones specified in its style, if it has one.

        public void ResetMaterials()
        {
            if (style != null)
                meshRenderer.sharedMaterials = style.defaultMaterials;
        }

        // Centers the pivot of this ProceduralSpline, by placing its transform at the average of its nodes.

        public void CenterPivot()
        {
            int n = nodes.Count;
            Vector3 average = Vector3.zero;

            for (int i = 0; i < n; i++)
                average += nodes[i].position;
            average /= n;

            for (int i = 0; i < n; i++)
                nodes[i] = new SplineNode(nodes[i].position - average, nodes[i].rotation, nodes[i].torsion);
            Regenerate();

            transform.position += transform.TransformVector(average);
        }

        // ==================================================================================================================== GENERATION
        
        // Regenerates the stored spline object (vertices, tangents, etc) from the current nodes and settings.

        private void RegenerateSpline()
        {
            s = new Spline(nodes, precision, closed, orientation);
        }

        // Regenerates this ProceduralSpline, calculating its highpoly mesh and collision meshes and applying them
        // to the GameObject. Called whenever the spline is edited and once on Awake when the spline loads ingame.

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

            // Generation

            if (style != null)
            {
                RegenerateSpline();

                meshFilter.sharedMesh = style.GenerateHighpolyMesh(spline);
                
                List<Mesh> collisionMeshes = style.GenerateCollisionMeshes(spline);

                if (collisionMeshes != null)
                {
                    for (int i = 0; i < collisionMeshes.Count; i++)
                    {
                        MeshCollider mc = gameObject.AddComponent<MeshCollider>();
                        mc.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning;
                        mc.sharedMaterial = style.defaultPhysicMaterials[i];
                        mc.sharedMesh = collisionMeshes[i];
                    }
                }
            }
        }

        // ====================================================================================================================

        LODResult ILODable.GetLOD()
        {
            RegenerateSpline();

            Mesh mesh = style.GenerateLowpolyMesh(spline);

            return new LODResult(transform.localToWorldMatrix, mesh, meshRenderer.sharedMaterials);
        }
    }
}