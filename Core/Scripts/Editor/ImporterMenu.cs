using System.IO;
using UnityEditor;
using UnityEngine;


namespace UniGLTF
{
    public static class ImporterMenu
    {
        [MenuItem(UniGLTFVersion.UNIGLTF_VERSION + "/Import", priority = 1)]
        public static void ImportMenu()
        {
            var path = UnityEditor.EditorUtility.OpenFilePanel("open gltf", "", "gltf,glb,zip");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            Debug.Log(path);

            var context = gltfImporter.Load(path);

            if (Application.isPlaying)
            {
                // load into scene
                context.ShowMeshes();
                Selection.activeGameObject = context.Root;
                return;
            }

            // import as asset
            try
            {
                var assetPath = UnityEditor.EditorUtility.SaveFilePanel("save prefab", "Assets", Path.GetFileNameWithoutExtension(path), "prefab");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                if (!assetPath.StartsWithUnityAssetPath())
                {
                    Debug.LogWarningFormat("out of asset path: {0}", assetPath);
                    return;
                }

                assetPath = assetPath.ToUnityRelativePath();
                context.SaveAsAsset(assetPath);

                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
            finally
            {
                // clear scene
                GameObject.DestroyImmediate(context.Root);
            }
        }
    }
}
