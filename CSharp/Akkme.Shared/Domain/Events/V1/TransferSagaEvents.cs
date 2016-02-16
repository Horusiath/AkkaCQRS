using Akkme.Shared.Domain.Accounts.TransferProtocol;
using Akkme.Shared.Infrastructure.Domain;
using Akkme.Shared.Infrastructure.Serializers;
using Bond;

namespace Akkme.Shared.Domain.Events.V1
{
    /// <summary>
    /// Marker interface used to recognize events, that should be handled by <see cref="TransferSaga"/>.
    /// They all use BondSerializer by default.
    /// </summary>
    public interface ITransferEvent : IDomainEvent, IBond { }

    /// <summary>
    /// Event marking begining of transfer process. It contain amount to be transfered as well as IDs of both accounts, 
    /// corresponding <see cref="TransferSaga"/> is working on.
    /// </summary>
    [Schema]
    public struct TransferStarted : ITransferEvent
    {
        [Id(0), Required] public readonly decimal Amount;
        [Id(1), Required] public readonly string FromAccountNr;
        [Id(2), Required] public readonly string ToAccountNr;

        public TransferStarted(decimal amount, string fromAccountNr, string toAccountNr) : this()
        {
            Amount = amount;
            FromAccountNr = fromAccountNr;
            ToAccountNr = toAccountNr;
        }
    }

    /// <summary>
    /// Event informing a <see cref="TransferSaga"/> that money has been successfully 
    /// withdrawn from <see cref="TransferStarted.FromAccountNr"/>.
    /// </summary>
    [Schema]
    public sealed class Withdrawn : ITransferEvent
    {
        public readonly string TransactionId;
        public readonly decimal Amount;

        public Withdrawn(string transactionId, decimal amount)
        {
            TransactionId = transactionId;
            Amount = amount;
        }
    }

    /// <summary>
    /// Event informing a <see cref="TransferSaga"/> that money has been successfully 
    /// deposited under <see cref="TransferStarted.ToAccountNr"/>.
    /// </summary>
    [Schema]
    public sealed class Deposited : ITransferEvent
    {
        public readonly string TransactionId;
        public readonly decimal Amount;

        public Deposited(string transactionId, decimal amount)
        {
            TransactionId = transactionId;
            Amount = amount;
        }
    }

    /// <summary>
    /// Event, that occurs if something wrong happens during <see cref="TransferSaga"/> process, 
    /// and compensating actions need to apply.
    /// </summary>
    [Schema]
    public sealed class Rollback : ITransferEvent
    {
        public static readonly Rollback Instance = new Rollback();

        private Rollback() { }

        public override bool Equals(object obj)
        {
            return obj is Rollback;
        }
    }

    /// <summary>
    /// Event informing a <see cref="TransferSaga"/>, that transfer has been finished successfully.
    /// </summary>
    [Schema]
    public sealed class Completed : ITransferEvent
    {
        public static readonly Completed Instance = new Completed();

        private Completed() { }

        public override bool Equals(object obj)
        {
            return obj is Completed;
        }
    }
}