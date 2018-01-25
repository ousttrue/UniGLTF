namespace UniGLTF
{
    public enum glComponentType: int
    {
        BYTE = 5120, // signed ?
        UNSIGNED_BYTE = 5121,

        SHORT = 5122,
        UNSIGNED_SHORT = 5123,

        //INT = 5124,
        UNSIGNED_INT = 5125,

        FLOAT = 5126,
    }

    public enum glBufferTarget : int
    {
        ARRAY_BUFFER = 34962,
        ELEMENT_ARRAY_BUFFER = 34963,
    }
}
