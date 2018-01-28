using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if NETFX_CORE
using System.Reflection;
using System.Runtime.Serialization;
#endif
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
using UnityEngine;
#endif


namespace UniGLTF
{
    public static class TypeExtensions
    {
        static HashSet<Type> m_serializableTypes = new HashSet<Type>
        {
            typeof(Byte),
            typeof(UInt16),
            typeof(UInt32),
            typeof(UInt64),
            typeof(SByte),
            typeof(Int16),
            typeof(Int32),
            typeof(Int64),
            typeof(Single),
            typeof(Double),
            typeof(String),
            typeof(Boolean),
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
            typeof(Quaternion),
            typeof(Vector4),
            typeof(Vector3),
            typeof(Vector2),
#endif
        };

        public static bool IsSerializable(this Type t)
        {
#if true
            if (m_serializableTypes.Contains(t))
            {
                // NETFX_CORE has no SerializableAttribute
                return true;
            }
#endif
            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                // collection
                return true;
            }
#if NETFX_CORE
            if (t.AttributeIsDefined<DataContractAttribute>())
            {
                // serializable and primitive
                return true;
            }
#else
            if (t.AttributeIsDefined<SerializableAttribute>())
            {
                // serializable and primitive
                return true;
            }
#endif
            if (t.IsEnum())
            {
                // enum
                return true;
            }
            /*
            if (!t.IsClass())
            {
                // struct
                return true;
            }
            */

            // class without serializable...
            return false;
        }

#if NETFX_CORE
        /// <summary>
        /// lhs = rhs が可能か
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool IsAssignableFrom(this Type lhs, Type rhs)
        {
            var lhsi = lhs.GetTypeInfo();
            var rhsi = rhs.GetTypeInfo();
            if (rhsi.IsSubclassOf(lhs))
            {
                return true;
            }
            if (rhsi.ImplementedInterfaces.Contains(lhs))
            {
                return true;
            }
            return false;
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this Type t)
        {
            return t.GetTypeInfo().GetCustomAttributes();
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this Type t, Type type, Boolean b)
        {
            return t.GetTypeInfo().GetCustomAttributes(type, b);
        }
        public static bool IsEnum(this Type t)
        {
            return t.GetTypeInfo().IsEnum;
        }
        public static bool IsClass(this Type t)
        {
            return t.GetTypeInfo().IsClass;
        }
        public static bool IsGenericType(this Type t)
        {
            return t.GetTypeInfo().IsGenericType;
        }
        public static bool IsInterface(this Type t)
        {
            return t.GetTypeInfo().IsInterface;
        }
        public static IEnumerable<Type> GetInterfaces(this Type t)
        {
            return t.GetTypeInfo().ImplementedInterfaces;
        }
        public static IEnumerable<Type> GetGenericArguments(this Type t)
        {
            return t.GetTypeInfo().GenericTypeArguments;
        }

        public static bool AttributeIsDefined<T>(this Type t)
            where T: Attribute
        {
            if (t == typeof(Single))
            {
                return true;
            }
            var ti = t.GetTypeInfo();
            return t.GetTypeInfo().GetCustomAttribute<T>() != null;
        }
#else
        public static bool IsEnum(this Type t)
        {
            return t.IsEnum;
        }
        public static bool IsClass(this Type t)
        {
            return t.IsClass;
        }
        public static bool IsGenericType(this Type t)
        {
            return t.IsGenericType;
        }
        public static bool IsInterface(this Type t)
        {
            return t.IsInterface;
        }

        public static bool AttributeIsDefined<T>(this Type t)
            where T: Attribute
        {
            return Attribute.IsDefined(t, typeof(T));
        }
#endif

        /*
        public static T ConvertTo<T>(object o)
        {
            try
            {
                if (typeof(T).IsAssignableFrom(o.GetType()))
                {
                    return (T)o;
                }
                else if (typeof(T).IsEnum())
                {
                    return (T)Convert.ChangeType(o, Enum.GetUnderlyingType(typeof(T)));
                }
                else
                {
                    return (T)Convert.ChangeType(o, typeof(T));
                }
            }
            catch (Exception ex)
            {
                return (T)o;
            }
        }
        */
    }
}
