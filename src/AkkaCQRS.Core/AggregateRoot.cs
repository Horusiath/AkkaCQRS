using System;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;

namespace AkkaCQRS.Core
{
    public interface IEntity<TIdentifier>
    {
        TIdentifier Id { get; set; }
    }

    public class AggregateUninitializedException<TAggregate> : Exception
    {
        public AggregateUninitializedException() { }
        public AggregateUninitializedException(string message) : base(message) { }
        protected AggregateUninitializedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public abstract class AggregateRoot<TEntity> : PersistentActor where TEntity : class, IEntity<Guid>
    {
        protected static readonly TimeSpan DefaultReceiveTimeout = TimeSpan.FromSeconds(10);
        private readonly string _persistenceId;

        /// <summary>
        /// Maximum number of events to occur in a row before state will be snapshoted.
        /// This is useful in scenarios, when actor may have to recover it's from potentially large number of events.
        /// This way snapshots are made automatically after specific number of events become persisted.
        /// </summary>
        public const int MaxEventsToSnapshot = 10;  // in real life this should be greater number
        private int _eventsSinceLastSnapshot = 0;

        private ILoggingAdapter _log;

        /// <summary>
        /// State describes custom data model used for each type of aggregate root.
        /// </summary>
        protected TEntity State = null;
        
        protected AggregateRoot(string persistenceId)
        {
            //Context.SetReceiveTimeout(DefaultReceiveTimeout);

            _persistenceId = persistenceId;
        }

        public override string PersistenceId { get { return _persistenceId; } }
        public ILoggingAdapter Log { get { return _log ?? (_log = Context.GetLogger()); } }

        protected override bool ReceiveRecover(object message)
        {
            if (message is SnapshotOffer)
            {
                var offeredState = ((SnapshotOffer) message).Snapshot as TEntity;
                if (offeredState != null)
                {
                    State = offeredState;
                }
            }
            else if (message is IEvent)
            {
                UpdateState(message as IEvent, null);
            }
            else return false;
            return true;
        }

        protected override bool ReceiveCommand(object message)
        {
            if (message is GetState)
            {
                Sender.Tell(State, Self);
                return true;
            }
            
            return OnCommand(message);
        }

        protected abstract bool OnCommand(object message);

        /// <summary>
        /// Wrapper method around persistence mechanisms. It persist an event, publishes it through event stream,
        /// calls aggregate state update and periodically performs snapshotting.
        /// </summary>
        protected void Persist(IEvent domainEvent, IActorRef sender = null)
        {
            Persist(domainEvent, e =>
            {
                UpdateState(domainEvent, sender);
                Publish(e);

                // if persisted events counter already exceeded the MaxEventsToSnapshot limit
                // snapshot will be automatically stored and counter will reset
                if ((_eventsSinceLastSnapshot++) >= MaxEventsToSnapshot)
                {
                    SaveSnapshot(State);
                    _eventsSinceLastSnapshot = 0;
                }
            });
        }

        protected void Publish(IEvent domainEvent)
        {
            Context.System.EventStream.Publish(domainEvent);
        }

        /// <summary>
        /// Update state is used for changing actor's internal state in response to incoming events.
        /// This method should be idempotent and should never call event persisting methods itself 
        /// nor generating another commands.
        /// 
        /// While in recovering mode, <paramref name="sender"/> is always null.
        /// </summary>
        protected abstract void UpdateState(IEvent domainEvent, IActorRef sender);
    }
}