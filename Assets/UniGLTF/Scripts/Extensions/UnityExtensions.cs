using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UniGLTF
{
    public struct PosRot
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public static PosRot FromGlobalTransform(Transform t)
        {
            return new PosRot
            {
                Position = t.position,
                Rotation = t.rotation,
            };
        }
    }

    public struct MeshWithMaterials
    {
        public Mesh Mesh;
        public Material[] Materials;
    }

    class BlendShape
    {
        public string Name;

        public BlendShape(string name)
        {
            Name = name;
        }

        public List<Vector3> Positions = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Vector3> Tangents = new List<Vector3>();
    }

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
            var path = new List<String>();
            for (var current = self; current != null; current = current.parent)
            {
                if (current == root)
                {
                    return String.Join("/", path.ToArray());
                }

                path.Insert(0, current.name);
            }

            throw new Exception("no RelativePath");
        }

        public static IEnumerable<Transform> GetChildren(this Transform self)
        {
            foreach (Transform child in self)
            {
                yield return child;
            }
        }

        public static IEnumerable<Transform> Traverse(this Transform t)
        {
            yield return t;
            foreach (Transform x in t)
            {
                foreach (Transform y in x.Traverse())
                {
                    yield return y;
                }
            }
        }

        public static Transform FindDescenedant(this Transform t, string name)
        {
            return t.Traverse().First(x => x.name == name);
        }

        public static IEnumerable<Transform> Ancestors(this Transform t)
        {
            yield return t;
            if (t.parent != null)
            {
                foreach (Transform x in t.parent.Ancestors())
                {
                    yield return x;
                }
            }
        }

        public static float[] ToArray(this Quaternion q)
        {
            return new float[] { q.x, q.y, q.z, q.w };
        }

        public static float[] ToArray(this Vector3 v)
        {
            return new float[] { v.x, v.y, v.z };
        }

        public static float[] ToArray(this Color c)
        {
            return new float[] { c.r, c.g, c.b, c.a };
        }

        public static void ReverseZ(this Transform root)
        {
            var globalMap = root.Traverse().ToDictionary(x => x, x => PosRot.FromGlobalTransform(x));

            foreach (var x in root.Traverse())
            {
                x.position = globalMap[x].Position.ReverseZ();
                x.rotation = globalMap[x].Rotation.ReverseZ();
            }
        }

        public static Mesh GetSharedMesh(this Transform t)
        {
            var meshFilter = t.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                return meshFilter.sharedMesh;
            }

            var skinnedMeshRenderer = t.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                return skinnedMeshRenderer.sharedMesh;
            }

            return null;
        }

        public static Material[] GetSharedMaterials(this Transform t)
        {
            var renderer = t.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.sharedMaterials;
            }

            return new Material[] { };
        }

        public static bool Has<T>(this Transform transform, T t)where T: Component
        {
            return transform.GetComponent<T>() == t;
        }
    }
}
