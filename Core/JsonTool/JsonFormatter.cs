using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq.Expressions;

namespace UniGLTF
{
    public class JsonFormatException : ArgumentException
    {
        public JsonFormatException(string msg) : base(msg) { }
    }

    public class JsonFormatter
    {
        IStore m_w;

        enum Current
        {
            ROOT,
            ARRAY,
            OBJECT
        }

        class Context
        {
            public Current Current;
            public int Count;

            public Context(Current current)
            {
                Current = current;
                Count = 0;
            }
        }

        Stack<Context> m_stack = new Stack<Context>();

        public JsonFormatter()
            : this(new StringBuilderStore(new StringBuilder()))
        { }

        public JsonFormatter(IStore w)
        {
            m_w = w;
            m_stack.Push(new Context(Current.ROOT));
        }

        public override string ToString()
        {
            var bytes = GetStore().Bytes;
            return Encoding.UTF8.GetString(bytes.Array, bytes.Offset, bytes.Count);
        }

        public IStore GetStore()
        {
            return m_w;
        }

        public void Clear()
        {
            m_w.Clear();
            m_stack.Clear();
            m_stack.Push(new Context(Current.ROOT));
        }

        void CommaCheck(bool isKey = false)
        {
            var top = m_stack.Pop();
            switch (top.Current)
            {
                case Current.ROOT:
                    {
                        if (top.Count != 0) throw new JsonFormatException("multiple root value");
                    }
                    break;

                case Current.ARRAY:
                    {
                        if (top.Count != 0)
                        {
                            m_w.Write(',');
                        }
                    }
                    break;

                case Current.OBJECT:
                    {
                        if (top.Count % 2 == 0)
                        {
                            if (!isKey) throw new JsonFormatException("key exptected");
                            if (top.Count != 0)
                            {
                                m_w.Write(',');
                            }
                        }
                        else
                        {
                            if (isKey) throw new JsonFormatException("key not exptected");
                        }
                    }
                    break;
            }
            top.Count += 1;
            /*
            {
                var debug = string.Format("{0} {1} = {2}", m_stack.Count, top.Current, top.Count);
                Debug.Log(debug);
            }
            */
            m_stack.Push(top);
        }

        public void Null()
        {
            CommaCheck();
            m_w.Write("null");
        }

        public void BeginList()
        {
            CommaCheck();
            m_w.Write('[');
            m_stack.Push(new Context(Current.ARRAY));
        }

        public void EndList()
        {
            m_w.Write(']');
            m_stack.Pop();
        }

        public void BeginMap()
        {
            CommaCheck();
            m_w.Write('{');
            m_stack.Push(new Context(Current.OBJECT));
        }

        public void EndMap()
        {
            m_w.Write('}');
            m_stack.Pop();
        }

        public void KeyValue<T>(Expression<Func<T>> expression)
        {
            var t = typeof(T);
            var body = expression.Body as MemberExpression;
            if (body == null)
            {
                body = ((UnaryExpression)expression.Body).Operand as MemberExpression;
            }
            Key(body.Member.Name);
            var func = expression.Compile();

            var formatterType = GetType();
            var method = formatterType.GetMethod("Value", new Type[] { typeof(T) });
            if (method == null)
            {
                // try IEnumerable<T>
                var generic_method = formatterType.GetMethods().First(x => x.Name == "Value" && x.IsGenericMethod);
                var g = t.GetGenericArguments()[0];
                method = generic_method.MakeGenericMethod(g);
            }

            method.Invoke(this, new object[] { func() });
        }

        public void Key(String key)
        {
            CommaCheck(true);
            m_w.Write(JsonString.Quote(key));
            m_w.Write(':');
        }

        public void Value(String key)
        {
            CommaCheck();
            m_w.Write(JsonString.Quote(key));
        }

        public void Value(Boolean x)
        {
            CommaCheck();
            m_w.Write(x ? "true" : "false");
        }

        /*
        public void Value<T>(T x) where T : struct, IConvertible
        {
            Value(Convert.ToInt32(x));
        }
        */

        public void Value(SByte x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }
        public void Value(Int16 x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }
        public void Value(Int32 x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }
        public void Value(Int64 x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }

        public void Value(Byte x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }
        public void Value(UInt16 x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }
        public void Value(UInt32 x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }
        public void Value(UInt64 x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }

        public void Value(Single x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }
        public void Value(Double x)
        {
            CommaCheck();
            m_w.Write(x.ToString());
        }
        public void Value(Vector3 v)
        {
            //CommaCheck();
            BeginMap();
            Key("x"); Value(v.x);
            Key("y"); Value(v.y);
            Key("z"); Value(v.z);
            EndMap();
        }

        public void Value(float[] a)
        {
            BeginList();
            foreach (var x in a)
            {
                Value(x);
            }
            EndList();
        }

        public void Value(int[] a)
        {
            BeginList();
            foreach (var x in a)
            {
                Value(x);
            }
            EndList();
        }

        public void Value(IJsonSerializable s)
        {
            CommaCheck();
            m_w.Write(s.ToJson());
        }

        public void Value<T>(IEnumerable<T> values)where T: IJsonSerializable
        {
            BeginList();
            foreach(var value in values)
            {
                Value(value);
            }
            EndList();
        }

        public void Bytes(ArraySegment<Byte> x)
        {
            CommaCheck();
            m_w.Write('"');
            m_w.Write(Convert.ToBase64String(x.Array, x.Offset, x.Count));
            m_w.Write('"');
        }

        public void Bytes(IEnumerable<byte> raw, int count)
        {
            Bytes(new ArraySegment<byte>(raw.Take(count).ToArray()));
        }

        public void Dump(ArraySegment<Byte> formated)
        {
            CommaCheck();
            m_w.Write(formated);
        }
    }
}
