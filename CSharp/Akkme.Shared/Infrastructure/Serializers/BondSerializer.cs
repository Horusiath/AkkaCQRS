using System;
using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Serialization;
using Bond;
using Bond.IO.Safe;
using Bond.Protocols;

namespace Akkme.Shared.Infrastructure.Serializers
{
    /// <summary>
    /// Marker interface used to resolve <see cref="BondSerializer"/> for typed using Microsoft Bond as serialization format.
    /// </summary>
    public interface IBond { }

    /// <summary>
    /// This is custom serializer using Microsoft Bond serialization library. In order to use it with standard akka serialization mechanism, 
    /// we defined <see cref="IBond"/> marker interface to be implemented by all types using this serializer as default one. 
    /// This also requires to define mappings in HOCON configuration - see Akkme.Shared/reference.conf file for more details.
    /// </summary>
    public class BondSerializer : Serializer
    {
        private readonly ConcurrentDictionary<Type, Serializer<FastBinaryWriter<OutputBuffer>>> _serializerCache =
            new ConcurrentDictionary<Type, Serializer<FastBinaryWriter<OutputBuffer>>>();

        private readonly ConcurrentDictionary<Type, Deserializer<FastBinaryReader<InputBuffer>>> _deserializerCache =
            new ConcurrentDictionary<Type, Deserializer<FastBinaryReader<InputBuffer>>>();

        private const int BufferSize = 1024;

        public override int Identifier => 9999;
        public override bool IncludeManifest => false;

        public BondSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        public override byte[] ToBinary(object obj)
        {
            var type = obj.GetType();
            var serializer = _serializerCache.GetOrAdd(type, t => new Serializer<FastBinaryWriter<OutputBuffer>>(t));
            var outputBuffer = new OutputBuffer(BufferSize);
            var writer = new FastBinaryWriter<OutputBuffer>(outputBuffer);
            serializer.Serialize(obj, writer);

            return outputBuffer.Data.Array;
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (type == null) throw new InvalidOperationException($"{GetType()}.FromBinary requires type to be provided");

            var deserializer = _deserializerCache.GetOrAdd(type, t => new Deserializer<FastBinaryReader<InputBuffer>>(t));
            var inputBuffer = new InputBuffer(bytes);
            var reader = new FastBinaryReader<InputBuffer>(inputBuffer);
            var obj = deserializer.Deserialize(reader);
            return obj;
        }
    }
}