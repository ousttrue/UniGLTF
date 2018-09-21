using System;


namespace UniGLTF
{
    public static class gltfImporter
    {
        [Obsolete("Use ImporterContext.Load(path)")]
        public static ImporterContext Load(string path)
        {
            return ImporterContext.Load(path);
        }

        [Obsolete("Use ImporterContext.Parse(path, bytes)")]
        public static ImporterContext Parse(string path, Byte[] bytes)
        {
            return ImporterContext.Parse(path, bytes);
        }

        [Obsolete("use ImporterContext.Load()")]
        public static void Load(ImporterContext ctx)
        {
            ctx.Load();
        }
    }
}
