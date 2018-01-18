using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;


namespace UniGLTF
{
    enum ChunkType : UInt32
    {
    }

    struct GlbChunk
    {
        public ChunkType ChunkType;
        public ArraySegment<Byte> Bytes;
    }

    [ScriptedImporter(1, "glb")]
    public class glbImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.LogFormat("## glbImporter ##: {0}", ctx.assetPath);

            var baseDir = Path.GetDirectoryName(ctx.assetPath);
            var bytes = File.ReadAllBytes(ctx.assetPath);

            int pos = 0;
            if(Encoding.ASCII.GetString(bytes, 0, 4) != "glTF")
            {
                throw new Exception("invalid magic");
            }
            pos += 4;

            var version = BitConverter.ToUInt32(bytes, pos);
            if (version != 2.0f)
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

                var type = (ChunkType)BitConverter.ToUInt32(bytes, pos);
                pos += 4;

                chunks.Add(new GlbChunk
                {
                    ChunkType=type,
                    Bytes = new ArraySegment<byte>(bytes, (int)pos, (int)chunkDataSize)
                });

                pos += chunkDataSize;
            }

            if(chunks.Count!=2)
            {
                throw new Exception("unknown chunk count: "+chunks.Count);
            }

            var jsonBytes = chunks[0].Bytes;
            var json = Encoding.UTF8.GetString(jsonBytes.Array, jsonBytes.Offset, jsonBytes.Count);
            gltfImporter.Import(ctx,
                json, 
                chunks[1].Bytes);
        }
    }
}
