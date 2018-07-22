#if UNIGLTF_DEVELOP
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;


namespace UniGLTF
{
    public static class UniGLTFVersionMenu
    {
        const int MENU_PRIORITY = 99;
        static string path = "Assets/UniGLTF/Core/Scripts/UniGLTFVersion.cs";

        const string template = @"
namespace UniGLTF
{{
    public static partial class UniGLTFVersion
    {{
        public const int MAJOR = {0};
        public const int MINOR = {1};

        public const string VERSION = ""{0}.{1}"";
    }}
}}
";

        [MenuItem(UniGLTFVersion.UNIGLTF_VERSION + "/Increment", priority = MENU_PRIORITY)]
        public static void IncrementVersion()
        {
            var source = string.Format(template, UniGLTFVersion.MAJOR, UniGLTFVersion.MINOR + 1);
            File.WriteAllText(path, source);
            AssetDatabase.Refresh();
        }

        [MenuItem(UniGLTFVersion.UNIGLTF_VERSION + "/Decrement", priority = MENU_PRIORITY)]
        public static void DecrementVersion()
        {
            var source = string.Format(template, UniGLTFVersion.MAJOR, UniGLTFVersion.MINOR - 1);
            File.WriteAllText(path, source);
            AssetDatabase.Refresh();
        }

        static IEnumerable<string> EnumerateFiles(string path)
        {
            if (Path.GetFileName(path).StartsWith(".git"))
            {
                yield break;
            }

            if (Directory.Exists(path))
            {
                foreach (var child in Directory.GetFileSystemEntries(path))
                {
                    foreach (var x in EnumerateFiles(child))
                    {
                        yield return x;
                    }
                }
            }
            else
            {
                if (Path.GetExtension(path).ToLower() != ".meta")
                {
                    yield return path.Replace("\\", "/");
                }
            }
        }

        [MenuItem(UniGLTFVersion.UNIGLTF_VERSION + "/Export unitypackage", priority = MENU_PRIORITY)]
        public static void CreateUnityPackage()
        {
            var path = EditorUtility.SaveFilePanel(
                    "export package",
                    null,
                    string.Format("UniGLTF-{0}.unitypackage", UniGLTFVersion.VERSION),
                    "unitypackage");
            AssetDatabase.ExportPackage(EnumerateFiles("Assets/UniGLTF").ToArray()
                , path, ExportPackageOptions.Interactive);
        }
    }
}
#endif
