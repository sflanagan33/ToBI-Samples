using UnityEngine;
using System.Collections.Generic;

namespace TOBI.Procedural
{
    public static class PolygonRounding
    {
        public struct Result
        {
            public bool success;            // Did the rounding succeed?
            public string failureReason;    // If the rounding failed, why did it?

            public Polygon polygon;         // The new rounded polygon, including new points and normals
            public List<int> connections;   // How the rounded polygon's points connect to the original points (indices)

            public Result(Polygon polygon, List<int> connections)
            {
                success = true;
                failureReason = "";

                this.polygon = polygon;
                this.connections = connections;
            }

            public Result(string failureReason)
            {
                success = false;
                this.failureReason = failureReason;

                polygon = null;
                connections = null;
            }
        }

        // Cached local variables to avoid GC
        
        private static List<Point> points = new List<Point>();
        private static List<Vector2> normals = new List<Vector2>();
        private static List<int> connections = new List<int>();
        private static List<Point> intrudedPoints = new List<Point>();
        private static List<Point> extrudedPoints = new List<Point>();

        // This function attempts to define a round-edged version of this polygon with the given corner radius
        // and minimum vertex distance (which defines how many points must be used to round each corner)

        public static Result Round(Polygon polygon, float radius, float minimumVertexDistance)
        {
            // Degenerate cases

            if (!polygon.IsSimple())
                return new Result("The polygon is self-intersecting.");
            else if (!polygon.IsClockwise())
                return new Result("The polygon is inside out.");
            else if (radius <= 0 || minimumVertexDistance <= 0)
                return new Result("The rounding parameters are invalid.");

            // The result data, built up while this function runs
            
            points.Clear();
            normals.Clear();
            connections.Clear();
            intrudedPoints.Clear();
            extrudedPoints.Clear();

            // For every point in the original polygon:

            int n = polygon.points.Count;
            for (int i = 0; i < n; i++)
            {
                // Get the three points defining this corner

                Point pCurr = polygon.points[i];
                Point pPrev = polygon.points[(i - 1 + n) % n];
                Point pNext = polygon.points[(i + 1) % n];

                // Check to see if this corner can be rounded by the given radius (if it can't, abort)

                float lengthA = ((Vector3) (pCurr - pPrev)).magnitude;
                float lengthB = ((Vector3) (pCurr - pNext)).magnitude;
                float angle = Vector3.Angle(pCurr - pNext, pCurr - pPrev) * Mathf.Deg2Rad;
                float segment = radius / Mathf.Abs(Mathf.Tan(angle / 2));

                if (segment > lengthA || segment > lengthB)
                    return new Result("The given radius is too large for this polygon.");

                // Find the normal vectors for the two edges that meet at this corner

                Vector2 normalA = new Vector2(pPrev.y - pCurr.y, pCurr.x - pPrev.x).normalized * radius;
                Vector2 normalB = new Vector2(pCurr.y - pNext.y, pNext.x - pCurr.x).normalized * radius;

                // Calculate whether or not this corner is clockwise or counterclockwise

                bool cornerClockwise = Vector3.SignedAngle(pCurr - pNext, pCurr - pPrev, Vector3.up) > 0;

                // Calculate the number of points required to round this corner (minimum 2)

                float arcLength = (Vector2.Angle(normalA, normalB) / 180) * Mathf.PI * radius;
                int cornerDivisions = Mathf.Max(Mathf.RoundToInt(arcLength / minimumVertexDistance), 2);

                // Calculate the intrusion and extrusion points for this corner based on those normals

                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float x1 = pPrev.x + normalA.x * sign;
                    float y1 = pPrev.y + normalA.y * sign;
                    float x2 = pCurr.x + normalA.x * sign;
                    float y2 = pCurr.y + normalA.y * sign;

                    float x3 = pCurr.x + normalB.x * sign;
                    float y3 = pCurr.y + normalB.y * sign;
                    float x4 = pNext.x + normalB.x * sign;
                    float y4 = pNext.y + normalB.y * sign;

                    // If the altered edges are too close to parallel, this corner can't be rounded (abort)

                    float d = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
                    if (Mathf.Abs(d) < 0.01f)
                        return new Result("Three or more points on this polygon are in a straight line.");

                    // Otherwise, calculate and store the intersection point as an intruded or extruded point

                    float x = ((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / d;
                    float y = ((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / d;

                    if (sign == -1)
                        intrudedPoints.Add(new Point(x, y));
                    else
                        extrudedPoints.Add(new Point(x, y));
                }

                // Add points, normals, and connections to the new rounded polygon based on all of the data above

                for (int j = 0; j < cornerDivisions; j++)
                {
                    float t = (float) j / (cornerDivisions - 1);
                    Vector2 normal = Vector3.Slerp(normalA, normalB, t).normalized;

                    if (cornerClockwise)
                        points.Add(intrudedPoints[i] + normal * radius);
                    else
                        points.Add(extrudedPoints[i] - normal * radius);
                    normals.Add(normal);
                    connections.Add(i);
                }
            }

            // Success!

            return new Result(new Polygon(points, normals), connections);
        }
    }
}