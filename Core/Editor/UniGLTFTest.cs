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

    void _AssertAreEqual(GameObject l, GameObject r)
    {
        Assert.AreEqual(l.name, r.name);
        Assert.AreEqual(l.transform.localPosition, r.transform.localPosition);
        Assert.AreEqual(l.transform.localRotation, r.transform.localRotation);
        Assert.AreEqual(l.transform.localScale, r.transform.localScale);
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

            _AssertAreEqual(lt.Current.gameObject, rt.Current.gameObject);
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
                context.Json = gltf.ToJson();
                Debug.LogFormat("{0}", context.Json);
                gltfImporter.Import<glTF>(context, new ArraySegment<byte>());

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
}
