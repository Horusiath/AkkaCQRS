using Akka.Actor;
using Akka.Configuration;

namespace Akkme.Shared.Infrastructure
{
    public class AkkmeSettings
    {
        public static AkkmeSettings Create(ActorSystem system)
        {
            return Create(system.Settings.Config.GetConfig("akkme"));
        }

        private static AkkmeSettings Create(Config config)
        {
            return new AkkmeSettings(
                snapshotAfter: config.GetInt("snapshot-after"));
        }

        public AkkmeSettings(int snapshotAfter)
        {
            SnapshotAfter = snapshotAfter;
        }

        /// <summary>
        /// Number of event's to occur since last snapshot before next snapshot should be made.
        /// </summary>
        public int SnapshotAfter { get; }
    }
}