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
                    var dataChunk=default(ArraySegment<byte>);
                    if (ext == ".gltf")
                    {
                        context.ParseJson<glTF>(File.ReadAllText(context.Path, System.Text.Encoding.UTF8), new ArraySegment<byte>());
                        gltfImporter.Import<glTF>(context);
                    }
                    else if (ext == ".glb")
                    {
                        context.ParseGlb<glTF>(File.ReadAllBytes(context.Path));

                        //
                        // https://answers.unity.com/questions/647615/how-to-update-import-settings-for-newly-created-as.html
                        //
                        for (int i = 0; i < context.GLTF.textures.Count; ++i)
                        {
                            var x = context.GLTF.textures[i];
                            var image = context.GLTF.images[x.source];
                            if (string.IsNullOrEmpty(image.uri))
                            {
                                // glb buffer
                                var folder = context.GetAssetFolder(".Textures").AssetPathToFullPath();
                                if (!Directory.Exists(folder))
                                {
                                    UnityEditor.AssetDatabase.CreateFolder(context.GLTF.baseDir, Path.GetFileNameWithoutExtension(context.Path) + ".Textures");
                                    //Directory.CreateDirectory(folder);
                                }

                                var textureName = string.IsNullOrEmpty(image.extra.name) ? string.Format("buffer#{0:00}", i) : image.extra.name;
                                var png = Path.Combine(folder, textureName + ".png");
                                var byteSegment = context.GLTF.GetViewBytes(image.bufferView);
                                File.WriteAllBytes(png, byteSegment.ToArray());
                                var assetPath = png.ToUnityRelativePath();
                                //Debug.LogFormat("import asset {0}", assetPath);
                                UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
                                UnityEditor.AssetDatabase.Refresh();
                                image.uri = assetPath.Substring(context.GLTF.baseDir.Length + 1);
                            }
                        }

                        EditorApplication.delayCall += () =>
                        {
                            // delay and can import png texture
                            gltfImporter.Import<glTF>(context);
                            context.SaveAsAsset();
                            context.Destroy(false);
                        };
                    }
                    else
                    {
                        continue;
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
}
