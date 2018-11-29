using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace UniGLTF
{
    public class GLTFJsonFormatter: UniJSON.JsonFormatter
    {
        public void GLTFValue(JsonSerializableBase s)
        {
            CommaCheck();
            Store.Write(s.ToJson());
        }

        public void GLTFValue<T>(IEnumerable<T> values) where T : JsonSerializableBase
        {
            BeginList();
            foreach (var value in values)
            {
                GLTFValue(value);
            }
            EndList();
        }

        public void GLTFValue(List<string> values)
        {
            BeginList();
            foreach (var value in values)
            {
                Value(value);
            }
            EndList();
        }

        protected override System.Reflection.MethodInfo GetMethod<T>(Expression<Func<T>> expression)
        {
            var t = typeof(T);
            var formatterType = GetType();

            {
                var method = formatterType.GetMethod("GLTFValue", new Type[] { typeof(T) });
                if (method != null)
                {
                    return method;
                }
            }

            // try IEnumerable<T>
            var generic_method = formatterType.GetMethods().First(x => x.Name == "GLTFValue" && x.IsGenericMethod);
            var ga = t.GetGenericArguments();
            if (ga.Length == 1)
            {
                var g = ga[0];
                var method = generic_method.MakeGenericMethod(g);
                if (method != null)
                {
                    return method;
                }
            }

            {
                var method = base.GetMethod(expression);
                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }
    }
}
