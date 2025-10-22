using MessagePack.Resolvers;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    public class CircularReferencesFormatter<T> : IMessagePackFormatter<T>
    {
        private const sbyte ReferenceExtensionTypeCode = 1;
        private readonly CircularReferencesResolver owner;
        private int serializedObjectsCount = 0;

        internal CircularReferencesFormatter(CircularReferencesResolver owner)
        {
            this.owner = owner;
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return default!;
            }

            if (reader.NextMessagePackType == MessagePackType.Extension)
            {
                var provisionaryReader = reader.CreatePeekReader();
                var extensionHeader = provisionaryReader.ReadExtensionFormatHeader();
                if (extensionHeader.TypeCode == ReferenceExtensionTypeCode)
                {
                    var id = provisionaryReader.ReadInt32();
                    reader = provisionaryReader;
                    return (T)ObjectReferencesHelper.DeserializedObjects[id];
                }
            }

            return this.owner.InnerResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            if (this.owner.SerializedObjects.TryGetValue(value, out var referenceId))
            {
                // This object has already been written. Skip it this time.
                int packLength = MessagePackWriter.GetEncodedLength(referenceId);
                writer.WriteExtensionFormatHeader(new ExtensionHeader(ReferenceExtensionTypeCode, packLength));
                writer.Write(referenceId);
                return;
            }

            this.owner.SerializedObjects.Add(value, serializedObjectsCount++);
            this.owner.InnerResolver.GetFormatterWithVerify<T>().Serialize(ref writer, value, options);
        }
    }
}
