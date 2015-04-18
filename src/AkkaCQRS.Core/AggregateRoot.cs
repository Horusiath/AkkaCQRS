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
        /// </summary>
        public const int MaxEventsToSnapshot = 10;
        private int _eventsSinceLastSnapshot = 0;

        private ILoggingAdapter _log;

        /// <summary>
        /// State describes custom data model used for each type of aggregate root.
        /// </summary>
        protected TEntity State = null;
        
        protected AggregateRoot(string persistenceId)
        {
            Context.SetReceiveTimeout(DefaultReceiveTimeout);

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

        protected abstract void UpdateState(IEvent domainEvent, IActorRef sender);
    }
}