using System;
using Akka;
using Akka.Persistence;
using Akkme.Shared.Infrastructure.Utils;

namespace Akkme.Shared.Infrastructure.Domain
{
    /// <summary>
    /// Base class used by all aggregate root types of actors.
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public abstract class AggregateRoot<TState> : ReceivePersistentActor
    {
        protected static readonly AkkmeSettings Settings = AkkmeSettings.Create(Context.System);
        protected Akkme Plugin { get; } = Akkme.Get(Context.System);
        protected TState State { get; set; }

        private int _eventCount = 0;

        protected AggregateRoot()
        {
            var path = Self.Path;
            PersistenceId = path.Parent.Name + "/" + path.Name;

            Recover<SnapshotOffer>(offer =>
            {
                if (offer.Snapshot is TState)
                    State = (TState) offer.Snapshot;
            });
            Recover((Action<IDomainEvent>)UpdateState);
        }

        public sealed override string PersistenceId { get; }

        public abstract void UpdateState(IDomainEvent domainEvent);

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
            _eventCount = (_eventCount + 1) % Settings.SnapshotAfter;
            if (_eventCount == 0)
            {
                SaveSnapshot(State);
            }
        }
    }
}