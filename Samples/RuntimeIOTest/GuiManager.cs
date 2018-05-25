using System.IO;
using UnityEngine;
using UnityEngine.UI;


namespace UniGLTF
{
    public class GuiManager : MonoBehaviour
    {
        [SerializeField]
        Button m_importButton;

        GameObject m_root;

#if UNITY_EDITOR
        void Start()
        {
            m_importButton.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            var path = UnityEditor.EditorUtility.OpenFilePanel("open gltf", "", "gltf,glb");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (m_root != null)
            {
                GameObject.Destroy(m_root);
            }

            var bytes = File.ReadAllBytes(path);

            Debug.LogFormat("[OnClick] {0}", path);
            var context = new ImporterContext();

            context.ParseGlb(bytes);

            gltfImporter.Import<glTF>(context);
            context.Root.name = Path.GetFileNameWithoutExtension(path);
            context.ShowMeshes();

            m_root = context.Root;
        }
#endif
    }
}
