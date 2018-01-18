using System.IO;
using System.Text;
using UnityEngine;


namespace UniGLTF {
    public class glTFRunTimeLoader : MonoBehaviour
    {
        void OnGUI()
        {
            if (GUILayout.Button("Open GLTF file"))
            {
#if UNITY_EDITOR
                var path = UnityEditor.EditorUtility.OpenFilePanel("open gltf", "", "gltf,glb");
                if (!string.IsNullOrEmpty(path))
                {
                    Debug.Log(path);
                    var bytes = File.ReadAllBytes(path);
                    var ext = Path.GetExtension(path).ToLower();
                    switch (ext)
                    {
                        case ".gltf":
                            {
                                var json = Encoding.UTF8.GetString(bytes);
                                var root = gltfImporter.Import(path, json);
                            }
                            break;

                        case ".glb":
                            {
                                var root = glbImporter.Import(path, bytes);
                            }
                            break;

                        default:
                            Debug.LogWarningFormat("unknown ext: {0}", path);
                            break;
                    }
                }
#endif
            }
        }
    }
}
