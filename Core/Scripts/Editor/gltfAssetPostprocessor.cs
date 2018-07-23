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
                Import(path);
            }
        }

        public static void Import(string path)
        {
            ImporterContext context = new ImporterContext(path);
            var ext = Path.GetExtension(path).ToLower();
            try
            {
                var prefabPath = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + ".prefab";
                prefabPath = prefabPath.Replace("\\", "/");
                if (ext == ".gltf")
                {
                    context.ParseJson(File.ReadAllText(path, System.Text.Encoding.UTF8),
                        new FileSystemStorage(Path.GetDirectoryName(path)));
                    gltfImporter.Load(context);
                    context.SaveAsAsset(prefabPath);
                    context.Destroy(false);
                }
                else if (ext == ".glb")
                {
                    context.ParseGlb(File.ReadAllBytes(path));
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
