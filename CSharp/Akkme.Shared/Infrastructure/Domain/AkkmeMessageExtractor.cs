using Akka.Cluster.Sharding;

namespace Akkme.Shared.Infrastructure.Domain
{
    public class AkkmeMessageExtractor : HashCodeMessageExtractor
    {
        public AkkmeMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            var sharded = message as ISharded;
            return sharded != null ? sharded.AggregateId : string.Empty;
        }
    }
}