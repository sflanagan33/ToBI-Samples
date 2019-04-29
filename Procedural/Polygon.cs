using UnityEngine;
using System.Collections.Generic;

namespace TOBI.Procedural
{
    public class Polygon
    {
        public List<Point> points;
        public List<Vector2> normals;

        public Polygon(List<Point> points, bool calculateNormals)
        {
            this.points = points;
            if (calculateNormals)
                CalculateNormals();
        }

        public Polygon(List<Point> points, List<Vector2> normals)
        {
            this.points = points;
            this.normals = normals;
        }

        private void CalculateNormals()
        {
            int n = points.Count;
            normals = new List<Vector2>(n);

            for (int i = 0; i < n; i++)
            {
                Point pCurr = points[i];
                Point pPrev = points[(i - 1 + n) % n];
                Point pNext = points[(i + 1) % n];
                Vector2 normalA = new Vector2(pPrev.y - pCurr.y, pCurr.x - pPrev.x).normalized;
                Vector2 normalB = new Vector2(pCurr.y - pNext.y, pNext.x - pCurr.x).normalized;
                normals.Add(Vector3.Slerp(normalA, normalB, 0.5f).normalized);
            }
        }

        // Returns whether or not this polygon is simple, which means its lines must be non-intersecting.

        public bool IsSimple()
        {
            int n = points.Count;

            for (int i = 0; i < n - 2; i++)
            {
                Point a = points[i];
                Point b = points[i + 1];
                int end = (i == 0) ? n - 1 : n;

                for (int j = i + 2; j < end; j++)
                {
                    Point c = points[j];
                    Point d = points[(j + 1) % n];

                    if (IsSimpleHelper(a, c, d) != IsSimpleHelper(b, c, d)
                     && IsSimpleHelper(a, b, c) != IsSimpleHelper(a, b, d))
                        return false;
                }
            }

            return true;
        }

        // Helps with the above.

        private static bool IsSimpleHelper(Point a, Point b, Point c)
        {
            return (c.y - a.y) * (b.x - a.x) > (b.y - a.y) * (c.x - a.x);
        }

        // Returns whether or not this polygon has its points defined in a (mostly) clockwise direction.

        public bool IsClockwise()
        {
            float sum = 0;
            int n = points.Count;

            for (int i = 0; i < n; i++)
            {
                Point p1 = points[i];
                Point p2 = points[(i + 1) % n];

                sum += (p2.x - p1.x) * (p2.y + p1.y);
            }

            return sum > 0;
        }
    }
}