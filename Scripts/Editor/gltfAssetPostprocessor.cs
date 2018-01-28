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
                else if(ext == ".glb")
                {
                    ImportGltf(path, true);
                }
            }
        }

        static void ImportGltf(string srcPath, bool isGlb)
        {
            GameObject go = null;
            try
            {
                if (isGlb)
                {
                    go = glbImporter.Import(srcPath, File.ReadAllBytes(srcPath), true);
                }
                else
                {
                    go = gltfImporter.Import(srcPath, File.ReadAllText(srcPath, System.Text.Encoding.UTF8), new ArraySegment<byte>(), true);
                }
            }
            finally
            {
                if (go != null)
                {
                    GameObject.DestroyImmediate(go);
                }
            }
        }
    }
}
