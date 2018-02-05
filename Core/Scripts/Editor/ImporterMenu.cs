using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace UniGLTF
{
    public static class ImporterMenu
    {
        [MenuItem("Assets/gltf/import")]
        public static void ImportMenu()
        {
            var path = UnityEditor.EditorUtility.OpenFilePanel("open gltf", "", "gltf,glb");
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log(path);
                var bytes = File.ReadAllBytes(path);
                var ext = Path.GetExtension(path).ToLower();
                switch (ext)
                {
                    case ".gltf":
                        {
                            var json = Encoding.UTF8.GetString(bytes);
                            var root = gltfImporter.Import(new RuntimeContext(path), json, new ArraySegment<byte>());
                            if (root == null)
                            {
                                return;
                            }
                            root.name = Path.GetFileNameWithoutExtension(path);
                            Selection.activeGameObject = root;
                        }
                        break;

                    case ".glb":
                        {
                            var root = glbImporter.Import(new RuntimeContext(path), bytes);
                            if (root == null)
                            {
                                return;
                            }
                            root.name = Path.GetFileNameWithoutExtension(path);
                            Selection.activeGameObject = root;
                        }
                        break;

                    default:
                        Debug.LogWarningFormat("unknown ext: {0}", path);
                        break;
                }
            }
        }
    }
}
