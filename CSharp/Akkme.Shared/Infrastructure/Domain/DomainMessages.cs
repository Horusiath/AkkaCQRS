namespace Akkme.Shared.Infrastructure.Domain
{
    public interface IDomainMessage { }

    public interface IDomainEvent: IDomainMessage { }

    public interface IDomainCommand: IDomainMessage { }

    public interface IShardedMessage
    {
        string ShardId { get; }
        string EntityId { get; }
    }

    public struct ShardEnvelope<TMessage> : IShardedMessage
    {
        public string ShardId { get; }
        public string EntityId { get; }
        public TMessage Message { get; }

        public ShardEnvelope(string shardId, string entityId, TMessage message)
        {
            ShardId = shardId;
            EntityId = entityId;
            Message = message;
        }
    }
}