using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

            var task = CoroutineUtil.Run(() => ZipArchive.Parse(bytes));
            yield return task;
            if (task.Error != null)
            {
                throw task.Error;
            }

            Debug.LogFormat("done {0}", task.Result);
        }
    }
}
