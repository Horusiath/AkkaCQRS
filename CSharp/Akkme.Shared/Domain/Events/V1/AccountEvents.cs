using System;
using Akkme.Shared.Infrastructure.Domain;
using Akkme.Shared.Infrastructure.Serializers;
using Bond;

namespace Akkme.Shared.Domain.Events.V1
{
    public interface IAccountEvent : IDomainEvent { }

    [Schema]
    public struct ModifiedBalance : IBond, IAccountEvent
    {
        [Id(0), Required] public readonly string TransactionId;
        [Id(1), Required] public readonly decimal Delta;

        public ModifiedBalance(string transactionId, decimal delta)
        {
            TransactionId = transactionId;
            Delta = delta;
        }
    }

    [Schema]
    public struct TransferCreated : IBond, IAccountEvent
    {
        [Id(0), Required]
        public readonly string TransferId;

        public TransferCreated(string transferId)
        {
            TransferId = transferId;
        }
    }

    [Schema]
    public struct TransferFinished : IBond, IAccountEvent
    {
        [Id(0), Required]
        public readonly string TransferId;
        [Id(1)]
        public readonly Exception Exception;

        public TransferFinished(string transferId, Exception exception = null)
        {
            TransferId = transferId;
            Exception = exception;
        }
    }
}