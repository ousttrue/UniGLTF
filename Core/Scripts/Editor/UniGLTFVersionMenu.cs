using System.IO;
using UnityEditor;


namespace UniGLTF
{
    public static class UniGLTFVersionMenu
    {
        static string path = "Assets/UniGLTF/Core/Scripts/UniGLTFVersion.cs";

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
