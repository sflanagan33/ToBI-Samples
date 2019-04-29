using UnityEngine;
using System.Collections;

namespace TOBI.Procedural
{
    [CreateAssetMenu(fileName = "New Silhouette", menuName = "Procedural Spline Silhouette", order = 1)]
    public class ProceduralSplineSilhouette : ScriptableObject
    {
        public ProceduralSplineStrip[] strips;
    }

    [System.Serializable]
    public struct ProceduralSplineStrip
    {
        public ProceduralSplineStripPoint[] points;
        public int materialIndex;
    }

    [System.Serializable]
    public struct ProceduralSplineStripPoint
    {
        public float x;
        public float y;
        public float v;
    }
}