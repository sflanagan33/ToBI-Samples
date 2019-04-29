using UnityEngine;
using System.Collections.Generic;

namespace TOBI.Procedural
{
    public class Spline
    {
        public bool closed;
        public int count;
        public Vector3[] vertices;
        public Vector3[] tangents;
        public Quaternion[] rotations;
        public float[] lengths;

        public Spline(List<SplineNode> nodes, int precision, bool closed, SplineOrientation orientation)
        {
            if (precision < 1)
                precision = 1;

            int nodeCount = nodes.Count;

            this.closed = closed;
            count = precision * (closed ? nodeCount : nodeCount - 1) + 1;
            vertices = new Vector3[count];
            tangents = new Vector3[count];
            rotations = new Quaternion[count];
            lengths = new float[count];

            int x = 0;
            Vector3 up = Vector3.up;

            int iEnd = closed ? nodeCount : nodeCount - 1;
            for (int i = 0; i < iEnd; i++)
            {
                SplineNode s = nodes[i];
                SplineNode e = nodes[(i + 1) % nodeCount];

                Vector3 p0 = s.position;
                Vector3 p1 = e.position;
                Vector3 t0 = s.rotation * Vector3.forward * s.torsion;
                Vector3 t1 = e.rotation * Vector3.forward * e.torsion;
                Vector3 r0 = s.rotation * Vector3.right;
                Vector3 r1 = e.rotation * Vector3.right;

                int jEnd = precision + (i == iEnd - 1 ? 1 : 0);
                for (int j = 0; j < jEnd; j++)
                {
                    float t = (float) j / precision;

                    if (j == 0)
                        vertices[x] = s.position;
                    else
                        vertices[x] = CalculateSpline(p0, p1, t0, t1, t);

                    tangents[x] = CalculateSplineDerivative(p0, p1, t0, t1, t);

                    if (orientation == SplineOrientation.UseRoll)
                        up = Vector3.Cross(tangents[x], Vector3.Slerp(r0, r1, t));

                    if (tangents[x] != Vector3.zero)
                    {
                        rotations[x] = Quaternion.LookRotation(tangents[x], up);
                    }

                    if (x > 0)
                        lengths[x] = lengths[x - 1] + Vector3.Distance(vertices[x], vertices[x - 1]);

                    x++;
                }
            }
        }

        public SplineLerpResult Lerp(Transform root, float length)
        {
            int i = 0;
            float t = 0;
            float l = Mathf.Repeat(length, lengths[count - 1]);

            for (i = 0; i < count - 1; i++)
            {
                t = (l - lengths[i]) / (lengths[i + 1] - lengths[i]);
                if (t >= 0 && t <= 1)
                    break;
            }

            Vector3 p = Vector3.Lerp(vertices[i], vertices[i + 1], t);
            Quaternion q = Quaternion.Slerp(rotations[i], rotations[i + 1], t);

            SplineLerpResult r = new SplineLerpResult();
            r.position = root.TransformPoint(p);
            r.rotation = root.rotation * q;
            return r;
        }

        public float ClosestPositionOnSpline(Transform root, Vector3 point)
        {
            point = root.InverseTransformPoint(point);
            
            float closestDist = Mathf.Infinity;
            float result = 0;

            for (int i = 0; i < count - 1; i++)
            {
                Vector3 ap = vertices[i] - point;
                Vector3 ab = vertices[i + 1] - vertices[i];

                float t = Vector3.Dot(ap, ab) / ab.sqrMagnitude;

                Vector3 onLine = Vector3.zero;
                if (t < 0)
                    onLine = vertices[i];
                else if (t > 1)
                    onLine = vertices[i + 1];
                else
                    onLine = vertices[i] + ab * t;

                float dist = (point - onLine).sqrMagnitude;

                if (dist < closestDist)
                {
                    closestDist = dist;
                    result = Mathf.Lerp(lengths[i], lengths[i + 1], t);
                }
            }

            return result;
        }

        private static Vector3 CalculateSpline(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1, float t)
        {
            t = Mathf.Clamp01(t);
            float t3 = t * t * t;
            float t2 = t * t;

            return (2 * t3 - 3 * t2 + 1) * p0
                 + (t3 - 2 * t2 + t) * m0
                 + (-2 * t3 + 3 * t2) * p1
                 + (t3 - t2) * m1;
        }

        private static Vector3 CalculateSplineDerivative(Vector3 p0, Vector3 p1, Vector3 m0, Vector3 m1, float t)
        {
            t = Mathf.Clamp01(t);
            float t2 = t * t;

            return (6 * t2 - 6 * t) * p0
                 + (3 * t2 - 4 * t + 1) * m0
                 + (-6 * t2 + 6 * t) * p1
                 + (3 * t2 - 2 * t) * m1;
        }
    }

    public enum SplineOrientation
    {
        AlwaysUp, UseRoll
    }

    public struct SplineLerpResult
    {
        public Vector3 position;
        public Quaternion rotation;

        public SplineLerpResult(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }
}