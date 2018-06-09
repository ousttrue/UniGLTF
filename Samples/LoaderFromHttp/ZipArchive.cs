using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace UniGLTF
{

    enum CompressionMethod : ushort
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

    class ZipParseException : Exception
    {
        public ZipParseException(string msg) : base(msg)
        { }
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

        public CompressionMethod CompressionMethod
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

        class EOCD
        {
            public ushort NumberOfThisDisk;
            public ushort DiskWhereCentralDirectoryStarts;
            public ushort NumberOfCentralDirectoryRecordsOnThisDisk;
            public ushort TotalNumberOfCentralDirectoryRecords;
            public int SizeOfCentralDirectoryBytes;
            public int OffsetOfStartOfCentralDirectory;
            public string Comment;

            public override string ToString()
            {
                return string.Format("<EOCD records: {0}, offset: {1}, '{2}'>",
                    NumberOfCentralDirectoryRecordsOnThisDisk,
                    OffsetOfStartOfCentralDirectory,
                    Comment
                    );
            }

            static int FindEOCD(byte[] bytes)
            {
                for (int i = bytes.Length - 22; i >= 0; --i)
                {
                    if (bytes[i] == 0x50
                        && bytes[i + 1] == 0x4b
                        && bytes[i + 2] == 0x05
                        && bytes[i + 3] == 0x06)
                    {
                        return i;
                    }
                }

                throw new ZipParseException("EOCD is not found");
            }

            public static EOCD Parse(Byte[] bytes)
            {
                var pos = FindEOCD(bytes);
                using (var ms = new MemoryStream(bytes, pos, bytes.Length - pos, false))
                using (var r = new BinaryReader(ms))
                {
                    var sig = r.ReadInt32();
                    if (sig != 0x06054b50) throw new ZipParseException("invalid eocd signature: " + sig);

                    var eocd = new EOCD
                    {
                        NumberOfThisDisk = r.ReadUInt16(),
                        DiskWhereCentralDirectoryStarts = r.ReadUInt16(),
                        NumberOfCentralDirectoryRecordsOnThisDisk = r.ReadUInt16(),
                        TotalNumberOfCentralDirectoryRecords = r.ReadUInt16(),
                        SizeOfCentralDirectoryBytes = r.ReadInt32(),
                        OffsetOfStartOfCentralDirectory = r.ReadInt32(),
                    };

                    var commentLength = r.ReadUInt16();
                    var commentBytes = r.ReadBytes(commentLength);
                    eocd.Comment = Encoding.ASCII.GetString(commentBytes);

                    return eocd;
                }
            }
        }

        class CentralDirectoryFile
        {
            public Encoding Encoding = Encoding.UTF8;
            public Byte[] Bytes;
            public int Offset;

            public UInt16 VersionMadeBy;
            public UInt16 VersionNeededToExtract;
            public UInt16 GeneralPurposeBitFlag;
            public CompressionMethod CompressionMethod;
            public UInt16 FileLastModificationTime;
            public UInt16 FileLastModificationDate;
            public Int32 CRC32;
            public Int32 CompressedSize;
            public Int32 UncompressedSize;
            public UInt16 FileNameLength;
            public UInt16 ExtraFieldLength;
            public UInt16 FileCommentLength;
            public UInt16 DiskNumberWhereFileStarts;
            public UInt16 InternalFileAttributes;
            public Int32 ExternalFileAttributes;
            public Int32 RelativeOffsetOfLocalFileHeader;

            public string FileName
            {
                get
                {
                    return Encoding.GetString(Bytes,
                        Offset + 46,
                        FileNameLength);
                }
            }

            public ArraySegment<Byte> ExtraField
            {
                get
                {
                    return new ArraySegment<byte>(Bytes,
                        Offset + 46 + FileNameLength,
                        ExtraFieldLength);
                }
            }

            public string FileComment
            {
                get
                {
                    return Encoding.GetString(Bytes,
                        Offset + 46 + FileNameLength + ExtraFieldLength,
                        FileCommentLength);
                }
            }

            public int Length
            {
                get
                {
                    return 46 + FileNameLength + ExtraFieldLength + FileCommentLength;
                }
            }

            public override string ToString()
            {
                return string.Format("<file [{0}]{1}({2}/{3} {4})>",
                    RelativeOffsetOfLocalFileHeader,
                    FileName,
                    CompressedSize,
                    UncompressedSize,
                    CompressionMethod
                    );
            }

            public static CentralDirectoryFile Parse(byte[] bytes, ref int pos)
            {
                using (var ms = new MemoryStream(bytes, pos, bytes.Length - pos, false))
                using (var r = new BinaryReader(ms))
                {
                    var sig = r.ReadInt32();
                    if (sig != 0x02014b50) throw new ZipParseException("invalid central directory file signature: " + sig);

                    var f = new CentralDirectoryFile
                    {
                        Bytes = bytes,
                        Offset = pos,

                        VersionMadeBy = r.ReadUInt16(),
                        VersionNeededToExtract = r.ReadUInt16(),
                        GeneralPurposeBitFlag = r.ReadUInt16(),
                        CompressionMethod = (CompressionMethod)r.ReadUInt16(),
                        FileLastModificationTime = r.ReadUInt16(),
                        FileLastModificationDate = r.ReadUInt16(),
                        CRC32 = r.ReadInt32(),
                        CompressedSize = r.ReadInt32(),
                        UncompressedSize = r.ReadInt32(),
                        FileNameLength = r.ReadUInt16(),
                        ExtraFieldLength = r.ReadUInt16(),
                        FileCommentLength = r.ReadUInt16(),
                        DiskNumberWhereFileStarts = r.ReadUInt16(),
                        InternalFileAttributes = r.ReadUInt16(),
                        ExternalFileAttributes = r.ReadInt32(),
                        RelativeOffsetOfLocalFileHeader = r.ReadInt32(),
                    };

                    pos += f.Length;

                    return f;
                }
            }
        }

        List<CentralDirectoryFile> Entries = new List<CentralDirectoryFile>();

        public static ZipArchive Parse(byte[] bytes)
        {
            var eocd = EOCD.Parse(bytes);
            //Debug.LogFormat("eocd: {0}", eocd);

            var archive = new ZipArchive();

            var pos = eocd.OffsetOfStartOfCentralDirectory;
            for (int i = 0; i < eocd.NumberOfCentralDirectoryRecordsOnThisDisk; ++i)
            {
                var file = CentralDirectoryFile.Parse(bytes, ref pos);
                //Debug.LogFormat("{0}: {1}", i, file);

                archive.Entries.Add(file);
            }

            /*
            var r = new BytesReader(bytes);
            var sig = r.ReadInt32();
            if (sig != 0x04034b50)
            {
                throw new ZipParseException("is not zip archive");
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

            {
                Version = version,
                Flags=flags,
                CompressionMethod=(CompressionMethod)method,
                CompressedSize=compressedSize,
                UncompressedSize=uncompressedSize,
            };
            */

            return archive;
        }
    }
}
