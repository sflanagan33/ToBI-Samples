using UnityEngine;
using System.Collections.Generic;

namespace TOBI.Procedural
{
    public static class PolygonTriangulation
    {
        public struct Result
        {
            public Point a;     // The first vertex
            public Point b;     // The second vertex
            public Point c;     // The third vertex

            public Result(Point a, Point b, Point c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            }
        }

        private static List<Result> results = new List<Result>(512);
        private static List<Point> p = new List<Point>(512);

        public static List<Result> Triangulate(List<Point> points)
        {
            return TriangulateInternal(points);
        }
        
        private static List<Result> TriangulateInternal(List<Point> points)
        {
            if (points.Count < 3)
                return null;

            p.Clear();
            p.AddRange(points);
            results.Clear();

            while (p.Count > 3)
            {
                int n = p.Count;
                bool success = false;

                for (int i = 0; i < n; i++)
                {
                    int prev = (i - 1 + n) % n;
                    int cur = i;
                    int next = (i + 1) % n;
                    Point pPrev = p[prev];
                    Point pCur = p[cur];
                    Point pNext = p[next];

                    bool noPointsInside = true;
                    for (int j = 0; j < n; j++)
                    {
                        Point pJ = p[j];

                        if (j != prev && j != cur && j != next && PointInTriangle(pPrev, pCur, pNext, p[j]))
                        {
                            noPointsInside = false;
                            break;
                        }
                    }

                    if (noPointsInside)
                    {
                        Vector2 a = pCur - pPrev;
                        Vector2 b = pNext - pCur;
                        float angle = Mathf.Atan2(a.x * b.y - a.y * b.x, a.x * b.x + a.y * b.y);

                        if (angle < 0)
                        {
                            results.Add(new Result(pPrev, pCur, pNext));
                            p.RemoveAt(i);

                            success = true;
                            break;
                        }
                    }
                }

                if (!success)
                    return null;
            }
            
            results.Add(new Result(p[0], p[1], p[2]));
            return results;
        }

        private static bool PointInTriangle(Point a, Point b, Point c, Point p)
        {
            float x0 = c.x - a.x;
            float y0 = c.y - a.y;
            float x1 = b.x - a.x;
            float y1 = b.y - a.y;
            float x2 = p.x - a.x;
            float y2 = p.y - a.y;

            float dot00 = x0 * x0 + y0 * y0;
            float dot01 = x0 * x1 + y0 * y1;
            float dot02 = x0 * x2 + y0 * y2;
            float dot11 = x1 * x1 + y1 * y1;
            float dot12 = x1 * x2 + y1 * y2;

            float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            return (u >= 0) && (v >= 0) && (u + v < 1);
        }
    }
}