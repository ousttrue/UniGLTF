#if UNITY_2017_3_OR_NEWER
using System.IO;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;


namespace UniGLTF
{
#if USE_UNIGLTF_SCRIPTEDIMPORTER
    [ScriptedImporter(1, "glb")]
#endif
    public class glbScriptedImporter: ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## glbImporter ##: {0}", ctx.assetPath);

            var bytes = File.ReadAllBytes(ctx.assetPath);

            glbImporter.Import(new ScriptedImporterContext(ctx), bytes);
        }
    }
}
#endif