using UnityEngine;

namespace TOBI.Procedural
{
    [System.Serializable]
    public struct Point
    {
        public float x;
        public float y;

        public Point(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        // ====================================================

        public static implicit operator Vector3(Point p)
        {
            return new Vector3(p.x, 0, p.y);
        }

        public static implicit operator Point(Vector3 v)
        {
            return new Point(v.x, v.z);
        }

        public static Vector3 operator +(Point a, Vector3 b)
        {
            return new Vector3(a.x + b.x, b.y, a.y + b.z);
        }

        public static Vector3 operator -(Point a, Vector3 b)
        {
            return new Vector3(a.x - b.x, -b.y, a.y - b.z);
        }

        // ====================================================

        public static implicit operator Vector2(Point p)
        {
            return new Vector2(p.x, p.y);
        }

        public static implicit operator Point(Vector2 v)
        {
            return new Point(v.x, v.y);
        }

        public static Vector2 operator +(Point a, Vector2 b)
        {
            return new Vector2(a.x + b.x, a.y + b.y);
        }

        public static Vector2 operator -(Point a, Vector2 b)
        {
            return new Vector2(a.x - b.x, a.y - b.y);
        }

        // ====================================================

        public static Point operator +(Point a, Point b)
        {
            return new Point(a.x + b.x, a.y + b.y);
        }

        public static Point operator -(Point a, Point b)
        {
            return new Point(a.x - b.x, a.y - b.y);
        }

        public static Point operator *(Point p, int s)
        {
            return new Point(p.x * s, p.y * s);
        }

        public static Point operator *(Point p, float s)
        {
            return new Point(p.x * s, p.y * s);
        }

        public static Point operator /(Point p, int s)
        {
            return new Point(p.x / s, p.y / s);
        }

        public static Point operator /(Point p, float s)
        {
            return new Point(p.x / s, p.y / s);
        }
    }
}