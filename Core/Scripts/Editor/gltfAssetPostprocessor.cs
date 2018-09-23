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
                var ext = Path.GetExtension(path).ToLower();
                switch (ext)
                {
                    case ".gltf":
                    case ".glb":
                        {
                            var gltfPath = UnityPath.FromUnityPath(path);
                            var prefabPath = gltfPath.Parent.Child(gltfPath.FileNameWithoutExtension + ".prefab");
                            ImportAsset(path, ext, prefabPath);
                            break;
                        }
                }
            }
        }

        public static void ImportAsset(string src, string ext, UnityPath prefabPath)
        {
            if (!prefabPath.IsUnderAssetsFolder)
            {
                Debug.LogWarningFormat("out of asset path: {0}", prefabPath);
                return;
            }

            var context = new ImporterContext();
            var srcPath = UnityPath.FromFullpath(src);

            context.Parse(src);
            if (ext == ".gltf")
            {
                if (srcPath.IsUnderAssetsFolder)
                {
                    //
                    // Import from asset folder, use texture assets
                    //
                    context.CreateTextureItems(srcPath.Parent);
                }
                else
                {
                    //
                    // Import from external folder, save texture assets
                    //
                    context.SaveTexturesAsPng(prefabPath);
                }
            }
            else if (ext == ".glb")
            {
                //
                // Extract textures from glb
                //
                context.SaveTexturesAsPng(prefabPath);
            }
            else if(ext == ".zip")
            {
                //
                // Extract textures from zip
                //
                context.SaveTexturesAsPng(prefabPath);
            }
            else
            {
                Debug.LogWarningFormat("unknown ext: {0}", src);
                return;
            }

            ImportDelayed(context, prefabPath, src);
        }

        static void ImportDelayed(ImporterContext context, UnityPath prefabPath, string src)
        {
            EditorApplication.delayCall += () =>
                {
                    //
                    // after textures imported
                    //
                    try
                    {
                        context.Load();
                        context.SaveAsAsset(prefabPath);
                        context.Destroy(false);
                    }
                    catch (UniGLTFNotSupportedException ex)
                    {
                        Debug.LogWarningFormat("{0}: {1}",
                            src,
                            ex.Message
                            );
                        context.Destroy(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogErrorFormat("import error: {0}", src);
                        Debug.LogErrorFormat("{0}", ex);
                        context.Destroy(true);
                    }
                };
        }
    }
}
