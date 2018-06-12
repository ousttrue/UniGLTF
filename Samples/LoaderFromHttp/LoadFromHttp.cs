using System;
using System.Collections;
using System.Linq;
using System.Text;
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
            if(!string.IsNullOrEmpty(www.error))
            {
                Debug.LogWarningFormat("fail to download: {0}", www.error);
                yield break;
            }
            Debug.LogFormat("downloaded {0} bytes", bytes.Length);

            var task = CoroutineUtil.RunOnThread(() => Zip.ZipArchive.Parse(bytes));
            yield return task;
            if (task.Error != null)
            {
                throw task.Error;
            }
            var zipArchive = task.Result;
            Debug.LogFormat("done {0}", zipArchive);

            var gltf = zipArchive.Entries.FirstOrDefault(x => x.FileName.ToLower().EndsWith(".gltf"));
            if (gltf == null)
            {
                Debug.LogWarning("no gltf in archive");
                yield break;
            }

#if false
            var json = zipArchive.ExtractToString(gltf, Encoding.UTF8);
#else
            var jsonBytes = zipArchive.Extract(gltf);
            var json = Encoding.UTF8.GetString(jsonBytes);
#endif
            Debug.LogFormat("gltf json: {0}", json);

            var context = new UniGLTF.ImporterContext();
            context.ParseJson<glTF>(json, zipArchive);
            gltfImporter.Import<glTF>(context);
            context.ShowMeshes();
        }
    }
}
