using System;
using Akka.Persistence.Journal;
using V1 = Akkme.Shared.Domain.Events.V1;
using V2 = Akkme.Shared.Domain.Events.V2;

namespace Akkme.Shared.Infrastructure.EventAdapters
{
    public class AccountEventAdapter : IEventAdapter
    {
        public string Manifest(object evt)
        {
            return string.Empty;
        }

        public object ToJournal(object evt)
        {
            return evt; // identity mapping in journal direction
        }

        public IEventSequence FromJournal(object evt, string manifest)
        {
            // all V1 version events incoming from journal will be mapped into their V2 equivalents

            if (evt is V1.IAccountEvent)
            {
                if (evt is V1.ModifiedBalance)
                {
                    var v1 = (V1.ModifiedBalance)evt;
                    var v2 = new V2.ModifiedBalance(v1.TransactionId, v1.Delta, DateTime.MinValue);
                    return new SingleEventSequence(v2);
                }
            }

            return new SingleEventSequence(evt);
        }
    }
}