using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniGLTF
{
    public class gltfAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                ImporterContext context = null;
                var ext = Path.GetExtension(path).ToLower();
                try
                {
                    if (ext == ".gltf")
                    {
                        context = ImportGltf(path, false);
                    }
                    else if (ext == ".glb")
                    {
                        context = ImportGltf(path, true);
                    }
                    if (context != null)
                    {
                        context.SaveAsAsset();
                    }
                    if (context != null)
                    {
                        context.Destroy(false);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogErrorFormat("import error: {0}", path);
                    Debug.LogErrorFormat("{0}", ex);
                    if (context != null)
                    {
                        context.Destroy(true);
                    }
                }
            }
        }

        static ImporterContext ImportGltf(string srcPath, bool isGlb)
        {
            Debug.LogFormat("ImportGltf: {0}", srcPath);
            var context = new ImporterContext
            {
                Path = srcPath,
            };
            if (isGlb)
            {
                glbImporter.Import<glTF>(context, File.ReadAllBytes(srcPath));
            }
            else
            {
                context.Json = File.ReadAllText(srcPath, System.Text.Encoding.UTF8);
                gltfImporter.Import<glTF>(context, new ArraySegment<byte>());
            }
            return context;
        }
    }
}
