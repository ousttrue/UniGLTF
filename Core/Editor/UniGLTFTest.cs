using NUnit.Framework;
using System;
using UniGLTF;
using UnityEngine;


public class UniGLTFTest
{
    static GameObject CreateSimpelScene()
    {
        var root = new GameObject("gltfRoot").transform;

        var scene = new GameObject("scene0").transform;
        scene.SetParent(root, false);
        scene.localPosition = new Vector3(1, 2, 3);

        return root.gameObject;
    }

    void AssertAreEqual(Transform go, Transform other)
    {
        var lt = go.Traverse().GetEnumerator();
        var rt = go.Traverse().GetEnumerator();

        while (lt.MoveNext())
        {
            if (!rt.MoveNext())
            {
                throw new Exception("rt shorter");
            }

            MonoBehaviourComparator.AssertAreEquals(lt.Current.gameObject, rt.Current.gameObject);
        }

        if (rt.MoveNext())
        {
            throw new Exception("rt longer");
        }
    }

    [Test]
    public void UniGLTFSimpleSceneTest()
    {
        var go = CreateSimpelScene();
        var context = new ImporterContext();

        try
        {
            // export
            var gltf = new glTF();
            using (var exporter = new gltfExporter(gltf))
            {
                exporter.Prepare(go);
                exporter.Export();

                // import
                context.ParseJson<glTF>(gltf.ToJson(), new ArraySegment<byte>());
                Debug.LogFormat("{0}", context.Json);
                gltfImporter.Import<glTF>(context);

                AssertAreEqual(go.transform, context.Root.transform);
            }
        }
        finally
        {
            //Debug.LogFormat("Destory, {0}", go.name);
            GameObject.DestroyImmediate(go);
            context.Destroy(true);
        }
    }

    void BufferTest(int size, Byte[] init=null)
    {
        var storage = new ArrayByteBuffer(init);
        var buffer = new glTFBuffer(storage);

        var bytes = new ArraySegment<Byte>(new byte[size]);
        var view = buffer.Storage.Extend(bytes, glBufferTarget.NONE);

        Assert.AreEqual(size, buffer.byteLength);
    }

    [Test]
    public void BufferTest()
    {
        BufferTest(0);
        BufferTest(128);
        BufferTest(256);
    }
}
