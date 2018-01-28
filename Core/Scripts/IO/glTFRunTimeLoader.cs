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
                glbImporter.ImportMenu();
#endif
            }
        }
    }
}
