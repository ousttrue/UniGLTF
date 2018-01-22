using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UniGLTF
{
    [Serializable]
    public class gltfNode
    {
        public string name = "";
        public int[] children;
        public float[] matrix;
        public float[] translation;
        public float[] rotation;
        public float[] scale;
        public int mesh = -1;
        public int skin = -1;
        public int camera = -1;

        public static gltfNode Create(Transform x, List<Transform> nodes, List<Mesh> meshes, List<SkinnedMeshRenderer> skins)
        {
            var node = new gltfNode
            {
                name = x.name,
                children = x.transform.GetChildren().Select(y => nodes.IndexOf(y)).ToArray(),
                rotation = x.transform.localRotation.ToArray(),
                translation = x.transform.localPosition.ToArray(),
                scale = x.transform.localScale.ToArray(),
            };

            var meshFilter = x.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                node.mesh = meshes.IndexOf(meshFilter.sharedMesh);
            }

            var skinnredMeshRenderer = x.GetComponent<SkinnedMeshRenderer>();
            if (skinnredMeshRenderer != null)
            {
                node.mesh = meshes.IndexOf(skinnredMeshRenderer.sharedMesh);
                node.skin = skins.IndexOf(skinnredMeshRenderer);
            }

            return node;
        }

        public GameObject ToGameObject()
        {
            var go = new GameObject(name);

            //
            // transform
            //
            if (translation != null)
            {
                go.transform.localPosition = new Vector3(
                    translation[0],
                    translation[1],
                    translation[2]);
            }
            if (rotation != null)
            {
                go.transform.localRotation = new Quaternion(
                    rotation[0],
                    rotation[1],
                    rotation[2],
                    rotation[3]);
            }
            if (scale != null)
            {
                go.transform.localScale = new Vector3(
                    scale[0],
                    scale[1],
                    scale[2]);
            }
            if (matrix != null)
            {
                var values = matrix;
                var col0 = new Vector4(values[0], values[1], values[2], values[3]);
                var col1 = new Vector4(values[4], values[5], values[6], values[7]);
                var col2 = new Vector4(values[8], values[9], values[10], values[11]);
                var col3 = new Vector4(values[12], values[13], values[14], values[15]);
                var m = new Matrix4x4(col0, col1, col2, col3);
                go.transform.localRotation = m.rotation;
                go.transform.localPosition = m.GetColumn(3);
            }

            return go;
        }
    }

    public struct TransformWithSkin
    {
        public Transform Transform;
        public int? SkinIndex;

        /*
        static void CancelRotation(Transform t, Dictionary<Transform, Vector3> positionMap)
        {
            t.rotation = Quaternion.identity;
            t.position = positionMap[t];

            foreach(Transform child in t)
            {
                CancelRotation(child, positionMap);
            }
        }
        */
    }
}
