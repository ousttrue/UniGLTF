#if UNITY_2017_3_OR_NEWER
using System;
using System.IO;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;


namespace UniGLTF
{
#if USE_UNIGLTF_SCRIPTEDIMPORTER
    [ScriptedImporter(1, "gltf")]
#endif
    public class gltfScriptedImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## Importer ##: {0}", ctx.assetPath);

            var json = File.ReadAllText(ctx.assetPath, Encoding.UTF8);

            gltfImporter.Import(new ScriptedImporterContext(ctx), json, new ArraySegment<byte>());
        }
    }
}
#endif