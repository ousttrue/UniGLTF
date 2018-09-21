namespace UniGLTF
{
    class AnimationKeyframeData
    {
#if UNITY_EDITOR
        public float Time { get; set; }
        public delegate float[] ConverterFunc(float[] values);
        private ConverterFunc _converter;
        private float[] m_values;
        public float[] MValues
        {
            get { return m_values; }
        }

        private bool[] m_enterValues;
        public bool[] MEnterValues
        {
            get { return m_enterValues; }
        }

        public AnimationKeyframeData(int elementCount, ConverterFunc converter)
        {
            m_values = new float[elementCount];
            m_enterValues = new bool[elementCount];
            for (int i = 0; i < m_enterValues.Length; i++)
            {
                m_enterValues[i] = false;
            }
            _converter = converter;
        }

        public void SetValue(float src, int offset)
        {
            if (m_values.Length > offset)
            {
                m_values[offset] = src;
                m_enterValues[offset] = true;
            }
        }

        public virtual float[] GetRightHandCoordinate()
        {
            if (_converter != null)
            {
                return _converter(m_values);
            }
            else
            {
                return m_values;
            }
        }
#endif
    }
}