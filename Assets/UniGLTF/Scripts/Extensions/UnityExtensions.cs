using UnityEngine;


namespace UniGLTF
{
    public static class UnityExtensions
    {
        public static Vector3 ReverseZ(this Vector3 v)
        {
            return new Vector3(v.x, v.y, -v.z);
        }

        public static Vector2 ReverseY(this Vector2 v)
        {
            return new Vector2(v.x, -v.y);
        }

        public static Quaternion ReverseZ(this Quaternion q)
        {
            float angle;
            Vector3 axis;
            q.ToAngleAxis(out angle, out axis);
            return Quaternion.AngleAxis(-angle, ReverseZ(axis));
        }
    }
}
