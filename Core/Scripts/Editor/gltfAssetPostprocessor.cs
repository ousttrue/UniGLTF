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
                ImporterContext context = new ImporterContext
                {
                    Path = path,
                };
                var ext = Path.GetExtension(path).ToLower();
                try
                {
                    if (ext == ".gltf")
                    {
                        context.Json = File.ReadAllText(context.Path, System.Text.Encoding.UTF8);
                        gltfImporter.Import<glTF>(context, new ArraySegment<byte>());
                    }
                    else if (ext == ".glb")
                    {
                        glbImporter.Import<glTF>(context, File.ReadAllBytes(context.Path));
                    }
                    else
                    {
                        continue;
                    }

                    context.SaveAsAsset();
                    context.Destroy(false);
                }
                catch (UniGLTFNotSupportedException ex)
                {
                    Debug.LogWarningFormat("{0}: {1}",
                        path,
                        ex.Message
                        );
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
    }
}
