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
                Import(UnityPath.FromUnityPath(path));
            }
        }

        public static void Import(UnityPath gltfPath)
        {
            if (!gltfPath.IsUnderAssetsFolder)
            {
                throw new Exception();
            }

            ImporterContext context = new ImporterContext(gltfPath);
            var ext = gltfPath.Extension.ToLower();
            try
            {
                var prefabPath = gltfPath.Parent.Child(gltfPath.FileNameWithoutExtension + ".prefab");
                if (ext == ".gltf")
                {
                    context.ParseJson(File.ReadAllText(gltfPath.FullPath, System.Text.Encoding.UTF8),
                        new FileSystemStorage(gltfPath.Parent.FullPath));
                    gltfImporter.Load(context);
                    context.SaveAsAsset(prefabPath);
                    context.Destroy(false);
                }
                else if (ext == ".glb")
                {
                    context.ParseGlb(File.ReadAllBytes(gltfPath.FullPath));
                    context.SaveTexturesAsPng(prefabPath);
                    EditorApplication.delayCall += () =>
                    {
                            // delay and can import png texture
                            gltfImporter.Load(context);
                        context.SaveAsAsset(prefabPath);
                        context.Destroy(false);
                    };
                }
                else
                {
                    return;
                }
            }
            catch (UniGLTFNotSupportedException ex)
            {
                Debug.LogWarningFormat("{0}: {1}",
                    gltfPath,
                    ex.Message
                    );
            }
            catch (Exception ex)
            {
                Debug.LogErrorFormat("import error: {0}", gltfPath);
                Debug.LogErrorFormat("{0}", ex);
                if (context != null)
                {
                    context.Destroy(true);
                }
            }
        }
    }
}
