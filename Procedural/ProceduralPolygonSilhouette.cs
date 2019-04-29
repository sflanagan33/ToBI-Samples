using UnityEngine;
using System.Collections;

namespace TOBI.Procedural
{
    [CreateAssetMenu(fileName = "New Silhouette", menuName = "Procedural Polygon Silhouette", order = 1)]
    public class ProceduralPolygonSilhouette : ScriptableObject
    {
        private const float WIDTH_BUFFER = 0.1f;

        public ProceduralPolygonStrip[] strips;

        public float Width
        {
            get
            {
                float w = 0;
                foreach (ProceduralPolygonStrip s in strips)
                    w = Mathf.Max(w, s.MaxDistance);
                return w + WIDTH_BUFFER;
            }
        }

        public float Height
        {
            get
            {
                float maxBottom = 0;
                float minTop = 0;

                foreach (ProceduralPolygonStrip s in strips)
                {
                    foreach (ProceduralPolygonStripPoint p in s.points)
                    {
                        if (p.anchor == ProceduralPolygonStripPoint.Anchor.Bottom && p.height > maxBottom)
                            maxBottom = p.height;
                        else if (p.anchor == ProceduralPolygonStripPoint.Anchor.Top && p.height < minTop)
                            minTop = p.height;
                    }
                }

                return maxBottom - minTop;
            }
        }
    }
    
    [System.Serializable]
    public struct ProceduralPolygonStrip
    {
        public ProceduralPolygonStripPoint[] points;
        public float uShift;
        public ProceduralPolygonStripVBehavior vBehavior;
        public int materialIndex;
        public float overrideTextureSize;

        public float MaxDistance
        {
            get
            {
                float d = 0;
                foreach (ProceduralPolygonStripPoint p in points)
                    d = Mathf.Max(d, Mathf.Abs(p.distance));
                return d;
            }
        }
    }

    public enum ProceduralPolygonStripVBehavior
    {
        ByHeight, ByStripDistance
    }

    [System.Serializable]
    public struct ProceduralPolygonStripPoint
    {
        public Anchor anchor;
        public float distance;
        public float height;

        public enum Anchor
        {
            Bottom, Top
        }
    }
}