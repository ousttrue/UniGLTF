using System;
using System.Collections.Generic;


namespace UniGLTF
{
    [Serializable]
    public class glTFAttributes
    {
        public int POSITION = -1;
        public int NORMAL = -1;
        public int TEXCOORD_0 = -1;
        public int JOINTS_0 = -1;
        public int WEIGHTS_0 = -1;
    }

    [Serializable]
    public struct glTFPrimitives
    {
        public int mode;
        public int indices;
        public glTFAttributes attributes;
        public int material;
    }

    [Serializable]
    public struct glTFMesh
    {
        public string name;
        public List<glTFPrimitives> primitives;

        public glTFMesh(string _name)
        {
            name = _name;
            primitives = new List<glTFPrimitives>();
        }
    }
}
