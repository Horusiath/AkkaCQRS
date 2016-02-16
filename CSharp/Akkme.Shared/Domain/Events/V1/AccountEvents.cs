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
}