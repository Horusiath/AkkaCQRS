using Akka.Actor;
using Akka.Configuration;

namespace Akkme.Shared.Infrastructure.Domain
{
    public class AggregateSettings
    {
        public static AggregateSettings Create(ActorSystem system)
        {
            return Create(system.Settings.Config.GetConfig("akkme.aggregator"));
        }

        private static AggregateSettings Create(Config config)
        {
            return new AggregateSettings(
                snapshotInterval: config.GetInt("snapshot-interval"));
        }

        public AggregateSettings(int snapshotInterval)
        {
            SnapshotInterval = snapshotInterval;
        }

        /// <summary>
        /// Number of event's to occur since last snapshot before next snapshot should be made.
        /// </summary>
        public int SnapshotInterval { get; }
    }
}