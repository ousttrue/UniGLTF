using System;
using System.Collections;
using System.IO;
using UnityEngine;


namespace UniGLTF
{
    public class LoadFromHttp : MonoBehaviour
    {
        [SerializeField]
        string m_url = "http://localhost:8000/gltf.zip";

        IEnumerator Start()
        {
            Debug.LogFormat("get {0}", m_url);
            var www = new WWW(m_url);
            yield return www;

            var bytes = www.bytes;
            Debug.LogFormat("downloaded {0} bytes", bytes.Length);

            var task = CoroutineUtil.Run(() => ZipArchive.Parse(bytes));
            yield return task;
            if (task.Error != null)
            {
                throw task.Error;
            }

            Debug.LogFormat("done {0}", task.Result);
        }

        enum CompressinMethod : ushort
        {
            Stored = 0, // The file is stored (no compression)
            Shrink = 1, // The file is Shrunk
            Reduced1 = 2, // The file is Reduced with compression factor 1
            Reduced2 = 3, // The file is Reduced with compression factor 2
            Reduced3 = 4, // The file is Reduced with compression factor 3
            Reduced4 = 5, // The file is Reduced with compression factor 4
            Imploded = 6, // The file is Imploded
            Reserved = 7, // Reserved for Tokenizing compression algorithm
            Deflated = 8, // The file is Deflated
        }

        class ZipArchive
        {
            public short Version
            {
                get;
                private set;
            }

            public ushort Flags
            {
                get;
                private set;
            }

            public CompressinMethod CompressionMethod
            {
                get;
                private set;
            }

            public ushort LastModFileTime
            {
                get;
                private set;
            }

            public ushort LastModFileDate
            {
                get;
                private set;
            }

            public int CRC32
            {
                get;
                private set;
            }

            public int CompressedSize
            {
                get;
                private set;
            }

            public int UncompressedSize
            {
                get;
                private set;
            }

            public ushort FilenameLength
            {
                get;
                private set;
            }

            public ushort ExtraFieldLength
            {
                get;
                private set;
            }

            public override string ToString()
            {
                return string.Format("<ZIP:{0}:{1} {2}/{3}bytes>", Version, CompressionMethod, CompressedSize, UncompressedSize);
            }

            public static ZipArchive Parse(byte[] bytes)
            {
                var r = new BytesReader(bytes);
                var sig = r.ReadInt32();
                if (sig != 0x04034b50)
                {
                    throw new Exception("is not zip archive");
                }

                var version = r.ReadInt16();
                var flags = r.ReadUInt16();
                var method = r.ReadUInt16();
                r.ReadUInt16();
                r.ReadUInt16();
                var crc=r.ReadInt32();
                var compressedSize = r.ReadInt32();
                var uncompressedSize = r.ReadInt32();
                r.ReadUInt16();
                r.ReadUInt16();

                var archive = new ZipArchive
                {
                    Version = version,
                    Flags=flags,
                    CompressionMethod=(CompressinMethod)method,
                    CompressedSize=compressedSize,
                    UncompressedSize=uncompressedSize,
                };
                return archive;
            }
        }
    }
}
