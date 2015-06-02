using System;
using Akka.Actor;
using Akka.Actor.Dsl;
using AkkaCQRS.Core.Accounting;
using Xunit;

namespace AkkaCQRS.Tests.Accounting
{
    public class TransactionCoordinatorSpecs : BaseSpec
    {
        private readonly IActorRef _transaction;
        private readonly Guid _transactionId;

        public TransactionCoordinatorSpecs()
        {
            _transactionId = Guid.NewGuid();
            _transaction = ActorOf<TransactionCoordinator>();
        }

        [Fact]
        public void TransactionCoordinator_should_forward_transaction_init_to_all_participants()
        {
            var p1 = ActorOf(config => config.Receive<TransactionCoordinator.BeginTransaction>((transaction, context) => TestActor.Forward(transaction)));
            var p2 = ActorOf(config => config.Receive<TransactionCoordinator.BeginTransaction>((transaction, context) => TestActor.Forward(transaction)));

            var beginTransaction = new TransactionCoordinator.BeginTransaction(_transactionId, new[] { p1, p2 }, null);
            _transaction.Tell(beginTransaction);

            ExpectMsg<TransactionCoordinator.BeginTransaction>(e => e.TransactionId == _transactionId);
            ExpectMsg<TransactionCoordinator.BeginTransaction>(e => e.TransactionId == _transactionId);
            ExpectNoMsg();
        }

        [Fact]
        public void TransactionCoordinator_after_receiving_continue_from_all_participants_should_send_commit()
        {
            Action<IActorDsl> configure = config =>
                {
                    config.Receive<TransactionCoordinator.BeginTransaction>((transaction, context) => context.Sender.Tell(new TransactionCoordinator.Continue(transaction.TransactionId)));
                    config.Receive<TransactionCoordinator.Commit>((commit, context) => TestActor.Forward(commit));
                };

            var p1 = ActorOf(configure);
            var p2 = ActorOf(configure);

            var beginTransaction = new TransactionCoordinator.BeginTransaction(_transactionId, new[] { p1, p2 }, null);
            _transaction.Tell(beginTransaction);

            ExpectMsg<TransactionCoordinator.Commit>(e => e.TransactionId == _transactionId);
            ExpectMsg<TransactionCoordinator.Commit>(e => e.TransactionId == _transactionId);
            ExpectNoMsg();
        }

        [Fact]
        public void TransactionCoordinator_after_receiving_any_abort_should_send_rollback()
        {
            var p1 = ActorOf(config =>
                {
                    config.Receive<TransactionCoordinator.BeginTransaction>((transaction, context) => context.Sender.Tell(new TransactionCoordinator.Continue(transaction.TransactionId)));
                    config.Receive<TransactionCoordinator.Commit>((commit, context) => TestActor.Forward(commit));
                    config.Receive<TransactionCoordinator.Rollback>((rollback, context) => TestActor.Forward(rollback));
                });
            var p2 = ActorOf(config =>
                {
                    config.Receive<TransactionCoordinator.BeginTransaction>((transaction, context) => 
                        context.Sender.Tell(new TransactionCoordinator.Abort(transaction.TransactionId, new Exception("boom"))));
                    config.Receive<TransactionCoordinator.Commit>((commit, context) => TestActor.Forward(commit));
                    config.Receive<TransactionCoordinator.Rollback>((rollback, context) => TestActor.Forward(rollback));
                });

            var beginTransaction = new TransactionCoordinator.BeginTransaction(_transactionId, new[] { p1, p2 }, null);
            _transaction.Tell(beginTransaction);

            ExpectMsg<TransactionCoordinator.Rollback>(e => e.TransactionId == _transactionId);
            ExpectMsg<TransactionCoordinator.Rollback>(e => e.TransactionId == _transactionId);
            ExpectNoMsg();
        }
    }
}