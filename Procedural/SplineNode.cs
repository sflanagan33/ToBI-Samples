using UnityEngine;
using System.Collections;

namespace TOBI.Procedural
{
    [System.Serializable]
    public struct SplineNode
    {
        public Vector3 position;
        public Quaternion rotation;
        public float torsion;

        public SplineNode(Vector3 position, Quaternion rotation, float torsion)
        {
            this.position = position;
            this.rotation = rotation;
            this.torsion = torsion;
        }
    }
}