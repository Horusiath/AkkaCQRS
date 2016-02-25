using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Persistence;
using Akka.TestKit.Xunit2;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using V1 = Akkme.Shared.Domain.Events.V1;
using V2 = Akkme.Shared.Domain.Events.V2;

namespace Akkme.Shared.Tests.Domain.Accounts
{
    public class AccountEventVersioningSpec : TestKit
    {
        /// <summary>
        /// Persistent actor for testing purposes. It's only able to handle V2 version events.
        /// </summary>
        private sealed class TestPersistentActor : ReceivePersistentActor
        {
            public override string PersistenceId { get; }

            public List<decimal> replayed = new List<decimal>();

            public TestPersistentActor(string persistenceId)
            {
                PersistenceId = persistenceId;
                Recover<V2.ModifiedBalance>(modified => replayed.Add(modified.Delta));
                Command<string>(s => s == "get-state", _ => Sender.Tell(replayed));
            }
        }

        // specify event adapter able to convert V1 event to V2
        public const string TestConfig = @"
            akka.persistence.journal.inmem {
                event-adapters {
                    v1tov2 = ""Akkme.Shared.Infrastructure.EventAdapters.AccountEventAdapter, Akkme.Shared""
                }
                event-adapter-bindings {
                    ""Akkme.Shared.Domain.Events.V1.IAccountEvent, Akkme.Shared"" = v1tov2
                }
            }
        ";

        public AccountEventVersioningSpec(ITestOutputHelper output) : base(TestConfig, output)
        {
        }

        [Fact]
        public void ModifiedBalance_should_be_replayed_always_in_V2()
        {
            const string pid = "account-1";

            // write some events to simulate existing multiversioned event stream
            WriteEvents(pid,
                new V1.ModifiedBalance(TransactionId(), 10),
                new V1.ModifiedBalance(TransactionId(), 11),
                new V1.ModifiedBalance(TransactionId(), 12),
                new V2.ModifiedBalance(TransactionId(), 13, DateTime.UtcNow),
                new V2.ModifiedBalance(TransactionId(), 14, DateTime.UtcNow));

            // create persistent actor. It's state will be replayed from journal
            var pref = ActorOf(Props.Create(() => new TestPersistentActor(pid)));
            pref.Tell("get-state", TestActor);

            // verify if all stored events were replayed
            ExpectMsg<List<decimal>>().ShouldAllBeEquivalentTo(new[] { 10, 11, 12, 13, 14 });
        }

        private void WriteEvents(string pid, params object[] events)
        {
            var journal = Persistence.Instance.Apply(Sys).JournalFor(null);
            var probe = CreateTestProbe();

            // prepare domain events to meat journal protocol criteria
            var messages = events
                .Zip(Counter(), (e, i) => new KeyValuePair<int, object>(i, e))
                .Select(kv => new Persistent(kv.Value, kv.Key, string.Empty, pid, false, probe.Ref)).ToArray();

            // feed journal with arbitrary events
            journal.Tell(new WriteMessages(messages, probe.Ref, 1));

            // validate that all messages have been stored
            probe.ExpectMsg<WriteMessagesSuccessful>();
            for (int i = 1; i <= events.Length; i++)
            {
                var n = i;
                probe.ExpectMsg<WriteMessageSuccess>(x => x.Persistent.SequenceNr == n && x.Persistent.PersistenceId == pid);
            }
        }

        private string TransactionId() => Guid.NewGuid().ToString("N");

        private IEnumerable<int> Counter()
        {
            var i = 1;
            while (true)
                yield return i++;
        }
    }
}