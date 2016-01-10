using System;
using Akka;
using Akka.Persistence;

namespace Akkme.Shared.Infrastructure.Domain
{
    /// <summary>
    /// Base class used by all aggregate root types of actors.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public abstract class AggregateRoot<TState> : PersistentActor
    {
        protected static readonly AggregateSettings AggregateSettings = AggregateSettings.Create(Context.System);
        protected TState State { get; set; }

        private int _eventCount = 0;

        protected AggregateRoot()
        {
            PersistenceId = Self.Path.Name + "-" + Context.Parent.Path.Name;
        }

        public sealed override string PersistenceId { get; }

        public abstract void UpdateState(IDomainEvent domainEvent);

        protected override bool ReceiveRecover(object message)
        {
            return message.Match()
                .With<SnapshotOffer>(offer =>
                {
                    if (offer.Snapshot is TState)
                        State = (TState) offer.Snapshot;
                })
                .With<IDomainEvent>(UpdateState)
                .WasHandled;
        }

        protected void Emit<TEvent>(TEvent domainEvent, Action<TEvent> handler = null) where TEvent : IDomainEvent
        {
            Persist(domainEvent, e =>
            {
                UpdateState(e);
                SaveSnapshotIfNecessary();
                handler?.Invoke(e);
            });
        }

        private void SaveSnapshotIfNecessary()
        {
            _eventCount = (_eventCount + 1)%AggregateSettings.SnapshotInterval;
            if (_eventCount == 0)
            {
                SaveSnapshot(State);
            }
        }
    }
}