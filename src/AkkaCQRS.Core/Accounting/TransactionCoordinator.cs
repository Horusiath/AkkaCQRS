using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Akka;
using Akka.Actor;
using Akka.Configuration;

namespace AkkaCQRS.Core.Accounting
{
    public class ActorTerminatedException : Exception
    {
        public readonly IActorRef TerminatedRef;

        public ActorTerminatedException(IActorRef terminatedRef)
        {
            TerminatedRef = terminatedRef;
        }

        protected ActorTerminatedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    public class TransactionAbortedException : Exception
    {
        public TransactionAbortedException() { }
        protected TransactionAbortedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    internal class TransactionSettings
    {
        public readonly TimeSpan TransactionTimeout;

        public TransactionSettings(Config config)
        {
            TransactionTimeout = config.GetTimeSpan("transaction-timeout");
        }
    }

    /// <summary>
    /// Distributed transaction coordinator used for two-phase committing. 
    /// Algorithm taken from: http://en.wikipedia.org/wiki/Two-phase_commit_protocol
    /// 
    /// Coordinator                                         Participants
    ///                         BEGIN TRANSACTION
    ///                 -------------------------------->
    ///                         CONTINUE/ABORT              prepare*/abort*
    ///                 <-------------------------------
    /// commit*/abort*          COMMIT/ROLLBACK
    ///                 -------------------------------->
    ///                         ACKNOWLEDGMENT              commit*/abort*
    ///                 <--------------------------------  
    /// end
    /// </summary>
    public class TransactionCoordinator : ActorBase
    {
        #region messages

        public interface ITransactionMessage
        {
            Guid TransactionId { get; }
        }

        /// <summary>
        /// Message send to all transaction participant to begin transaction process.
        /// In response each participant should reply with either <see cref="Continue"/> 
        /// to continue the transaction or <see cref="Abort"/> to stop it.
        /// </summary>
        [Serializable]
        public sealed class BeginTransaction : ITransactionMessage
        {
            public readonly Guid Id;
            public readonly IEnumerable<IActorRef> Participants;
            public readonly object Payload;

            public BeginTransaction(Guid id, IEnumerable<IActorRef> participants, object payload)
            {
                Id = id;
                Participants = participants;
                Payload = payload;
            }

            public Guid TransactionId { get { return Id; } }
        }

        /// <summary>
        /// Message send by transaction participant in response to <see cref="BeginTransaction"/>.
        /// Allows to continue transaction process. Followed by <see cref="Commit"/> message sent by 
        /// transaction coordinator.
        /// </summary>
        [Serializable]
        public sealed class Continue : ITransactionMessage
        {
            public readonly Guid Id;

            public Continue(Guid transactionId)
            {
                Id = transactionId;
            }

            public Guid TransactionId { get { return Id; } }
        }

        /// <summary>
        /// Message send by transaction participant in response to <see cref="BeginTransaction"/>.
        /// Stops the transaction process. Followed by <see cref="Rollback"/> message sent by 
        /// transaction coordinator.
        /// </summary>
        [Serializable]
        public sealed class Abort : ITransactionMessage
        {
            public readonly Guid Id;
            public readonly Exception Reason;

            public Abort(Guid transactionId, Exception reason)
            {
                Id = transactionId;
                Reason = reason;
            }

            public Guid TransactionId { get { return Id; } }
        }

        /// <summary>
        /// Message sent to all transaction participants after first phase has ended successfully.
        /// Followed by <see cref="Ack"/> message send to coordinator by transaction participants.
        /// </summary>
        [Serializable]
        public sealed class Commit : ITransactionMessage
        {
            public readonly Guid Id;

            public Commit(Guid transactionId)
            {
                Id = transactionId;
            }

            public Guid TransactionId { get { return Id; } }
        }

        /// <summary>
        /// Message sent to all transaction participants after first phase has ended with failure on any of them. 
        /// Followed by <see cref="Ack"/> message send to coordinator by transaction participants.
        /// </summary>
        [Serializable]
        public sealed class Rollback : ITransactionMessage
        {
            public readonly Guid Id;
            public readonly Exception Reason;

            public Rollback(Guid transactionId, Exception reason)
            {
                Id = transactionId;
                Reason = reason;
            }

            public Guid TransactionId { get { return Id; } }
        }

        /// <summary>
        /// Acknowledge message sent by transaction participants to end transaction.
        /// </summary>
        [Serializable]
        public sealed class Ack : ITransactionMessage
        {
            public readonly Guid Id;

            public Ack(Guid transactionId)
            {
                Id = transactionId;
            }

            public Guid TransactionId { get { return Id; } }
        }

        #endregion

        private Guid _currentTransactionId;

        private readonly TransactionSettings _settings;
        private readonly ISet<IActorRef> _participants;
        private ISet<IActorRef> _phaseOnePending;
        private ISet<IActorRef> _phaseTwoPending;

        public TransactionCoordinator()
        {
            _settings = new TransactionSettings(Context.System.Settings.Config.GetConfig("akka.akka-cqrs"));

            // set inactivity timeout to be able to send rollback to transaction participants
            SetReceiveTimeout(_settings.TransactionTimeout);

            _currentTransactionId = Guid.Empty;
            _participants = new HashSet<IActorRef> { };
            Become(Ready);
        }

        protected bool Ready(object message)
        {
            return Receive(message) || message.Match()
                .With<BeginTransaction>(tx =>
                {
                    _currentTransactionId = tx.TransactionId;

                    var participants = new HashSet<IActorRef>(tx.Participants);
                    _participants.UnionWith(participants);
                    _phaseOnePending = participants;

                    foreach (var participant in participants)
                    {
                        participant.Tell(tx);

                        // coordinator has to be aware of participants termination
                        Context.Watch(participant);
                    }

                    _phaseTwoPending = new HashSet<IActorRef> { };
                    Become(PhaseOne);
                })
                .WasHandled;
        }

        protected bool PhaseOne(object message)
        {
            var wasHandled = Receive(message);
            if (!wasHandled)
            {
                var transaction = message as ITransactionMessage;
                if (transaction != null && transaction.TransactionId == _currentTransactionId)
                {
                    return transaction.Match()
                        .With<Continue>(cont =>
                        {
                            // wait for all participants to confirm
                            _phaseTwoPending.Add(Sender);
                            _phaseOnePending.Remove(Sender);

                            // when all participants confirmed, send Commit and go to phase two
                            if (_phaseOnePending.Count == 0)
                            {
                                var commit = new Commit(_currentTransactionId);
                                foreach (var participant in _phaseTwoPending)
                                {
                                    participant.Tell(commit);
                                }

                                Become(PhaseTwo);
                            }
                        })
                        .With<Abort>(_ =>
                        {
                            var rollback = new Rollback(_currentTransactionId, new TransactionAbortedException());
                            foreach (var participant in _participants)
                            {
                                participant.Tell(rollback);
                            }

                            Become(PhaseTwo);
                        })
                        .WasHandled;
                }
            }

            return wasHandled;
        }

        protected bool PhaseTwo(object message)
        {
            return Receive(message) || message.Match()
                .With<Ack>(ack =>
                {
                    if (ack.TransactionId == _currentTransactionId)
                    {
                        _phaseTwoPending.Remove(Sender);

                        // when all expected Acks returned, go to initial state
                        if (_phaseTwoPending.Count == 0)
                        {
                            _participants.Clear();
                            _phaseOnePending.Clear();
                            _phaseTwoPending.Clear();
                            _currentTransactionId = Guid.Empty;

                            Become(Ready);
                        }
                    }
                    else Unhandled(ack);
                })
                .WasHandled;
        }

        protected override bool Receive(object message)
        {
            var terminated = message as Terminated;
            if (terminated != null)
            {
                if (_currentTransactionId == Guid.Empty)
                {
                    var rollback = new Rollback(_currentTransactionId, new ActorTerminatedException(terminated.ActorRef));
                    _participants.ExceptWith(new[] { terminated.ActorRef });

                    foreach (var participant in _participants)
                    {
                        participant.Tell(rollback);
                    }

                    Become(PhaseTwo);

                    return true;
                }
            }
            else if (message is ReceiveTimeout)
            {
                foreach (var participant in _participants)
                {
                    participant.Tell(new Rollback(_currentTransactionId, new Exception(string.Format("Transaction {0} has reached it's ACK timeout", _currentTransactionId))));
                }
                
                Context.Stop(Self);
            }

            return false;
        }
    }
}