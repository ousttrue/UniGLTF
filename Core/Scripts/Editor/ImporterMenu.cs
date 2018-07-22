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

            context.ShowMeshes();
            Selection.activeGameObject = context.Root;
        }
    }
}
