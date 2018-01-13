using System;
using System.Collections.Generic;
using UnityEngine;


namespace UniGLTF
{
    public static class UnityExtensions
    {
        public static Vector4 ReverseZ(this Vector4 v)
        {
            return new Vector4(v.x, v.y, -v.z, v.w);
        }

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

        public static Matrix4x4 ReverseZ(this Matrix4x4 m)
        {
#if true
            m.SetTRS(m.GetColumn(3).ReverseZ(), m.rotation.ReverseZ(), Vector3.one);
            return m;
#else
            return new Matrix4x4(m.GetColumn(0), m.GetColumn(1), -m.GetColumn(2), m.GetColumn(3));
#endif
        }

        public static string RelativePathFrom(this Transform self, Transform root)
        {
            var path=new List<String>();
            for(var current=self; current!=null; current=current.parent)
            {
                if (current == root)
                {
                    return String.Join("/", path.ToArray());
                }

                path.Insert(0, current.name);
            }

            throw new Exception("no RelativePath");
        }
    }
}
