using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;


namespace UniGLTF
{
    public static class gltfExporter
    {
        const string CONVERT_HUMANOID_KEY = "GameObject/gltf/export";
        private static readonly UnityEngine.Object json;

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

            var buffer = new ArrayByteBuffer();
            var gltf = glTF.FromGameObject(go, buffer);

            var json = gltf.ToJson();
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            using (var s = new FileStream(path, FileMode.Create))
            {
                GlbHeader.WriteTo(s);

                var pos = s.Position;
                s.Position += 4; // skip total size

                int size = 12;

                {
                    var chunk = new GlbChunk(json);
                    size += chunk.WriteTo(s);
                }
                {
                    var chunk = new GlbChunk(buffer.GetBytes());
                    size += chunk.WriteTo(s);
                }

                s.Position = pos;
                var bytes = BitConverter.GetBytes(size);
                s.Write(bytes, 0, bytes.Length);
            }

            Debug.Log(json);
        }
    }
}
