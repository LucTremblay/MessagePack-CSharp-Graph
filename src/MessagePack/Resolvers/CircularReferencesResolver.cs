using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    public sealed class CircularReferencesResolver : IFormatterResolver
    {
        public readonly IFormatterResolver InnerResolver;
        private readonly Dictionary<Type, IMessagePackFormatter> circularReferencesFormatters = [];
        public readonly Dictionary<object, int> SerializedObjects = [];

        public CircularReferencesResolver(IFormatterResolver inner)
        {
            this.InnerResolver = inner;
            ObjectReferencesHelper.DeserializedObjects = new ArrayList();
        }

        IMessagePackFormatter<T>? IFormatterResolver.GetFormatter<T>()
        {
            var type = typeof(T);
            if (!type.IsValueType && type.GetCustomAttributes(typeof(AllowCircularRefrerencesAttribute), false).Length != 0)
            {
                if (!this.circularReferencesFormatters.TryGetValue(type, out var formatter))
                {
                    formatter = new CircularReferencesFormatter<T>(this);
                    this.circularReferencesFormatters.Add(type, formatter);
                }

                return (IMessagePackFormatter<T>)formatter;
            }

            return this.InnerResolver.GetFormatter<T>();
        }
    }

    public class ObjectReferencesHelper
    {
        [ThreadStatic]
        public static ArrayList DeserializedObjects;

        public static void AddReference(object reference)
        {
            DeserializedObjects.Add(reference);
        }

        public static readonly MethodInfo AddReferenceMethodInfo = typeof(ObjectReferencesHelper).GetMethod(nameof(AddReference))!;

    }
}
