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
                var context = new ImporterContext
                {
                    Path = path,
                };
                var bytes = File.ReadAllBytes(path);
                var ext = Path.GetExtension(path).ToLower();
                switch (ext)
                {
                    case ".gltf":
                        {
                            context.Json = Encoding.UTF8.GetString(bytes);
                            gltfImporter.Import(context, new ArraySegment<byte>());
                            context.Root.name = Path.GetFileNameWithoutExtension(path);
                            Selection.activeGameObject = context.Root;
                        }
                        break;

                    case ".glb":
                        {
                            glbImporter.Import(context, bytes);
                            context.Root.name = Path.GetFileNameWithoutExtension(path);
                            Selection.activeGameObject = context.Root;
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
