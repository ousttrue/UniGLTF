using System;


namespace UniGLTF
{
    public class UniGLTFException : Exception
    {
        public UniGLTFException(string fmt, params object[] args) : this(string.Format(fmt, args)) { }
        public UniGLTFException(string msg) : base(msg) { }
    }

    public class DracoIsNotSupportedException : UniGLTFException
    {
        public const string message = "draco is not supported";
        public DracoIsNotSupportedException() : base(message) { }
    }
}
