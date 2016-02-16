using System;
using Akkme.Shared.Infrastructure.Serializers;
using Bond;

namespace Akkme.Shared.Infrastructure.Domain
{
    public interface IDomainMessage { }

    public interface IDomainEvent: IDomainMessage { }

    public interface IDomainCommand: IDomainMessage { }

    public interface ISharded
    {
        string AggregateId { get; }
    }

    [Schema]
    public struct ShardEnvelope<TMessage> : IBond, ISharded
    {
        [Id(0), Required] public string AggregateId { get; }
        [Id(0), Required] public TMessage Message { get; }
        
        public ShardEnvelope(string aggregateId, TMessage message)
        {
            AggregateId = aggregateId;
            Message = message;
        }
    }
}