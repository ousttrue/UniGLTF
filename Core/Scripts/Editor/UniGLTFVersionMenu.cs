using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniGLTF
{
    public static class UniGLTFVersionMenu
    {
        static string FindUniGLTF(string path)
        {
            if (path.EndsWith("/UniGLTF"))
            {
                return path;
            }

            foreach (var x in Directory.GetDirectories(path))
            {
                var dir = FindUniGLTF(x.Replace("\\", "/"));
                if (!string.IsNullOrEmpty(dir))
                {
                    return dir;
                }
            }

            return null;
        }

        static string m_path;
        static string path
        {
            get
            {
                if (string.IsNullOrEmpty(m_path))
                {
                    m_path = FindUniGLTF(Application.dataPath.Replace("\\", "/")) + "/Core/Scripts/UniGLTFVersion.cs";
                }
                return m_path;
            }
        }

        const string template = @"
namespace UniGLTF
{{
    public static class UniGLTFVersion
    {{
        public const int MAJOR = {0};
        public const int MINOR = {1};

        public const string VERSION = ""{0}.{1}"";

        public const string DecrementMenuName = ""UniGLTF/Version({0}.{1}) Decrement"";
        public const string IncrementMenuName = ""UniGLTF/Version({0}.{1}) Increment"";
    }}
}}
";

#if UNIGLTF_DEVELOP
        [MenuItem(UniGLTFVersion.IncrementMenuName)]
#endif
        public static void IncrementVersion()
        {
            var source = string.Format(template, UniGLTFVersion.MAJOR, UniGLTFVersion.MINOR + 1);
            File.WriteAllText(path, source);
            AssetDatabase.Refresh();
        }

#if UNIGLTF_DEVELOP
        [MenuItem(UniGLTFVersion.DecrementMenuName)]
#endif
        public static void DecrementVersion()
        {
            var source = string.Format(template, UniGLTFVersion.MAJOR, UniGLTFVersion.MINOR - 1);
            File.WriteAllText(path, source);
            AssetDatabase.Refresh();
        }
    }
}
