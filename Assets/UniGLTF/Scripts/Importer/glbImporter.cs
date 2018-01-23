using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;


namespace UniGLTF
{
    [ScriptedImporter(1, "glb")]
    public class glbImporter : ScriptedImporter
    {
        public const string GLB_MAGIC = "glTF";
        public const float GLB_VERSION = 2.0f;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## glbImporter ##: {0}", ctx.assetPath);

            var bytes = File.ReadAllBytes(ctx.assetPath);

            Import(new gltfImporter.Context(ctx), bytes);
        }

        public static GameObject Import(string path, Byte[] bytes)
        {
            return Import(new gltfImporter.Context(path), bytes);
        }

        public static GameObject Import(gltfImporter.Context ctx, Byte[] bytes)
        {
            var baseDir = Path.GetDirectoryName(ctx.Path);

            int pos = 0;
            if(Encoding.ASCII.GetString(bytes, 0, 4) != GLB_MAGIC)
            {
                throw new Exception("invalid magic");
            }
            pos += 4;

            var version = BitConverter.ToUInt32(bytes, pos);
            if (version != GLB_VERSION)
            {
                throw new Exception("unknown version: " + version);
            }
            pos += 4;

            var totalLength = BitConverter.ToUInt32(bytes, pos);
            pos += 4;

            var chunks = new List<GlbChunk>();
            while(pos<bytes.Length)
            {
                var chunkDataSize = BitConverter.ToInt32(bytes, pos);
                pos += 4;

                //var type = (GlbChunkType)BitConverter.ToUInt32(bytes, pos);
                var type = Encoding.ASCII.GetString(bytes, pos, 4);
                pos += 4;

                chunks.Add(new GlbChunk
                {
                    ChunkType = (GlbChunkType)Enum.Parse(typeof(GlbChunkType), type),
                    Bytes = new ArraySegment<byte>(bytes, (int)pos, (int)chunkDataSize)
                });

                pos += chunkDataSize;
            }

            if(chunks.Count!=2)
            {
                throw new Exception("unknown chunk count: "+chunks.Count);
            }

            if (chunks[0].ChunkType != GlbChunkType.JSON)
            {
                throw new Exception("chunk 0 is not JSON");
            }

            if (chunks[1].ChunkType != GlbChunkType.BIN)
            {
                throw new Exception("chunk 1 is not BIN");
            }

            var jsonBytes = chunks[0].Bytes;
            var json = Encoding.UTF8.GetString(jsonBytes.Array, jsonBytes.Offset, jsonBytes.Count);

            return gltfImporter.Import(ctx,
                json, 
                chunks[1].Bytes);
        }
    }
}
