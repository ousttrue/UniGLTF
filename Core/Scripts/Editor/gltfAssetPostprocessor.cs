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
                        Import(UnityPath.FromUnityPath(path));
                        break;
                }
            }
        }

        public static void Import(UnityPath gltfPath)
        {
            if (!gltfPath.IsUnderAssetsFolder)
            {
                throw new Exception();
            }

            var context = new ImporterContext();
            var ext = gltfPath.Extension.ToLower();

            var prefabPath = gltfPath.Parent.Child(gltfPath.FileNameWithoutExtension + ".prefab");
            context.Parse(gltfPath.FullPath);
            if (ext == ".gltf")
            {
                var textureBaseDir = gltfPath.Parent;
                context.SetTextureBaseDir(textureBaseDir);
                textureBaseDir.ImportAsset();
            }
            else if (ext == ".glb")
            {
                // save texture assets !
                context.SaveTexturesAsPng(prefabPath);
            }
            else
            {
                return;
            }

            ImportDelayed(context, prefabPath, gltfPath);
        }

        static void ImportDelayed(ImporterContext context, UnityPath prefabPath, UnityPath gltfPath)
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
                            gltfPath,
                            ex.Message
                            );
                        context.Destroy(true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogErrorFormat("import error: {0}", gltfPath);
                        Debug.LogErrorFormat("{0}", ex);
                        context.Destroy(true);
                    }
                };
        }
    }
}
