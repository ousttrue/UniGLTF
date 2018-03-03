using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniGLTF
{
    public class gltfAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                var ext = Path.GetExtension(path).ToLower();
                if (ext == ".gltf")
                {
                    ImportGltf(path, false);
                }
                else if (ext == ".glb")
                {
                    ImportGltf(path, true);
                }
            }
        }

        static void ImportGltf(string srcPath, bool isGlb)
        {
            using (var context = new PrefabContext(srcPath))
            {
                try
                {
                    if (isGlb)
                    {
                        glbImporter.Import(context, File.ReadAllBytes(srcPath));
                    }
                    else
                    {
                        gltfImporter.Import(context, File.ReadAllText(srcPath, System.Text.Encoding.UTF8), new ArraySegment<byte>());
                    }
                }
                catch (Exception)
                {
                    Debug.LogErrorFormat("import error: {0}", srcPath);
                    throw;
                }
            }
        }
    }
}
