using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace UniGLTF
{
    public static class gltfExporter
    {
        const string CONVERT_HUMANOID_KEY = "GameObject/gltf/export";
        [MenuItem(CONVERT_HUMANOID_KEY, true, 1)]
        private static bool ExportValidate()
        {
            return Selection.activeObject != null && Selection.activeObject is GameObject;
        }
        [MenuItem(CONVERT_HUMANOID_KEY, false, 1)]
        private static void Export()
        {
            var go = Selection.activeObject as GameObject;

            var path = EditorUtility.SaveFilePanel(
                    "Save glb",
                    "",
                    go.name + ".glb",
                    "glb");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var formatter = new JsonFormatter();

            formatter.BeginMap();
            formatter.EndMap();

            var jsonBytes = formatter.GetStore().Bytes;

            var chunks = new List<GlbChunk>();
            chunks.Add(new GlbChunk(GlbChunkType.JSON, jsonBytes));

            using (var s = new FileStream(path, FileMode.Create))
            {
                GlbHeader.WriteTo(s);

                s.Position += 4; // skip total size

                var pos = s.Position;

                int size = 0;
                foreach (var chunk in chunks)
                {
                    size+=s.Write(chunk);
                }

                s.Position = pos;
                var bytes = BitConverter.GetBytes(size);
                s.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
