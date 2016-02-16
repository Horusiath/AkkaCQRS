using System;
using Akkme.Shared.Infrastructure.Domain;
using Akkme.Shared.Infrastructure.Serializers;
using Bond;

namespace Akkme.Shared.Domain.Events.V2
{
    public interface IAccountEvent : IDomainEvent { }

    [Schema]
    public struct ModifiedBalance : IBond, IAccountEvent
    {
        [Id(0), Required] public readonly string TransactionId;
        [Id(1), Required] public readonly decimal Delta;
        [Id(2), Required] public readonly DateTime CreatedAt;

        public ModifiedBalance(string transactionId, decimal delta, DateTime createdAt)
        {
            TransactionId = transactionId;
            Delta = delta;
            CreatedAt = createdAt;
        }
    }
}