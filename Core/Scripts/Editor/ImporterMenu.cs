using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace UniGLTF
{
    public static class ImporterMenu
    {
        [MenuItem(UniGLTFVersion.UNIGLTF_VERSION + "/Import", priority = 1)]
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
                            context.ParseJson(Encoding.UTF8.GetString(bytes), new FileSystemStorage(Path.GetDirectoryName(path)));
                            gltfImporter.Load(context);
                            context.Root.name = Path.GetFileNameWithoutExtension(path);
                            context.ShowMeshes();
                            Selection.activeGameObject = context.Root;
                        }
                        break;

                    case ".glb":
                        {
                            context.ParseGlb(bytes);
                            gltfImporter.Load(context);
                            context.Root.name = Path.GetFileNameWithoutExtension(path);
                            context.ShowMeshes();
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
