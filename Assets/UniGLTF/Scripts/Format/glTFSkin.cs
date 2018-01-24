using System;
using UnityEngine;

namespace UniGLTF
{
    [Serializable]
    public struct glTFSkin
    {
        public int inverseBindMatrices;
        public int[] joints;
    }
}
