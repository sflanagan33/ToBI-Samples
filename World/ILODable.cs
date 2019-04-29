using UnityEngine;
using System.Collections;

namespace TOBI.World
{
    public interface ILODable
    {
        LODResult GetLOD();
    }

    public struct LODResult
    {
        public Matrix4x4 matrix;
        public Mesh mesh;
        public Material[] materials;

        public LODResult(Matrix4x4 matrix, Mesh mesh, Material[] materials)
        {
            this.matrix = matrix;
            this.mesh = mesh;
            this.materials = materials;
        }
    }
}