using System;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Akkme.Shared.Domain.Accounts.TransferProtocol;
using Akkme.Shared.Infrastructure.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Akkme.Shared.Tests.Domain.Accounts
{
    public class TransferProtocolSpec : TestKit
    {
        private readonly IActorRef transferSaga;
        private readonly TestProbe accountsShardRegionProbe;
        private readonly string transactionId = Guid.NewGuid().ToString("N");
        private readonly string toAccountNr = Guid.NewGuid().ToString("N");
        private readonly decimal amount = 10;

        public TransferProtocolSpec(ITestOutputHelper output) : base(output: output)
        {
            accountsShardRegionProbe = CreateTestProbe();
            transferSaga = Sys.ActorOf(Props.Create(() => new TransferSaga(transactionId, accountsShardRegionProbe.Ref)));
        }

        [Fact]
        public void TransferSaga_when_initialized_with_Transfer_first_sends_a_Withdraw_request_to_sender()
        {
            Phase1();
        }

        [Fact]
        public void TransferSaga_rollbacks_on_Withdrawal_request_failed()
        {
            Watch(transferSaga);
            Phase1();
            transferSaga.Tell(new WithdrawFailed(transactionId, new Exception("TEST")));
            ExpectMsg(new Deposit(transactionId, amount));
            ExpectMsg<TransferFailed>();
            ExpectTerminated(transferSaga);
        }

        [Fact]
        public void TransferSaga_after_WithdrawalSucceed_sends_Deposit_request_to_receiver()
        {
            Phase1();
            Phase2();
        }

        [Fact]
        public void TransferSaga_rollbacks_on_Deposit_request_failed()
        {
            Watch(transferSaga);
            Phase1();
            Phase2();
            transferSaga.Tell(new DepositFailed(transactionId, new Exception("TEST")));
            accountsShardRegionProbe.ExpectMsg(new ShardEnvelope<Withdraw>(toAccountNr, new Withdraw(transactionId, amount)));
            ExpectMsg(new Deposit(transactionId, amount));
            ExpectMsg<TransferFailed>();
            ExpectTerminated(transferSaga);
        }

        [Fact]
        public void TransferSaga_on_transfer_completed_shuts_down_itself()
        {
            Watch(transferSaga);
            Phase1();
            Phase2();
            transferSaga.Tell(new DepositSucceed(transactionId));
            ExpectMsg(new TransferSucceed(transactionId));
            ExpectTerminated(transferSaga);
        }

        private void Phase1()
        {
            transferSaga.Tell(new Transfer(transactionId, toAccountNr, amount), TestActor);
            ExpectMsg(new Withdraw(transactionId, amount));
        }

        private void Phase2()
        {
            transferSaga.Tell(new WithdrawSucceed(transactionId), TestActor);
            accountsShardRegionProbe.ExpectMsg(new ShardEnvelope<Deposit>(toAccountNr, new Deposit(transactionId, amount)));
        }
    }
}