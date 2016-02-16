using System;
using Akka.Actor;
using Akkme.Shared.Infrastructure.Domain;

namespace Akkme.Shared.Domain.Accounts.TransferProtocol
{
    public interface ITransactional
    {
        string TransactionId { get; }
    }

    public struct Withdraw : IDomainCommand, ITransactional
    {
        public string TransactionId { get; }
        public decimal Amount { get; }

        public Withdraw(string transactionId, decimal amount)
        {
            if (string.IsNullOrEmpty(transactionId)) throw new ArgumentNullException(nameof(transactionId), "Withdraw command require transaction id to be provided");
            if (amount <= 0M) throw new ArgumentException("Withdraw command require amount to be positive value", nameof(amount));

            TransactionId = transactionId;
            Amount = amount;
        }
    }

    public struct WithdrawSucceed : IDomainCommand, ITransactional
    {
        public string TransactionId { get; }
        public WithdrawSucceed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) throw new ArgumentNullException(nameof(transactionId), "WithdrawSucceed command require transaction id to be provided");

            TransactionId = transactionId;
        }
    }

    public struct WithdrawFailed: IDomainCommand, ITransactional
    {
        public string TransactionId { get; }
        public Exception Cause { get; }
        public WithdrawFailed(string transactionId, Exception cause)
        {
            if (string.IsNullOrEmpty(transactionId)) throw new ArgumentNullException(nameof(transactionId), "WithdrawFailed command require transaction id to be provided");

            TransactionId = transactionId;
            Cause = cause;
        }
    }

    public struct Deposit : IDomainCommand, ITransactional
    {
        public string TransactionId { get; }
        public decimal Amount { get; }

        public Deposit(string transactionId, decimal amount)
        {
            if (string.IsNullOrEmpty(transactionId)) throw new ArgumentNullException(nameof(transactionId), "Deposit command require transaction id to be provided");
            if (amount <= 0M) throw new ArgumentException("Deposit command require amount to be positive value", nameof(amount));

            TransactionId = transactionId;
            Amount = amount;
        }
    }

    public struct DepositSucceed : IDomainCommand, ITransactional
    {
        public string TransactionId { get; }

        public DepositSucceed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) throw new ArgumentNullException(nameof(transactionId), "DepositSucceed command require transaction id to be provided");

            TransactionId = transactionId;
        }
    }

    public struct DepositFailed : IDomainCommand, ITransactional
    {
        public string TransactionId { get; }
        public Exception Cause { get; }

        public DepositFailed(string transactionId, Exception cause)
        {
            if (string.IsNullOrEmpty(transactionId)) throw new ArgumentNullException(nameof(transactionId), "DepositFailed command require transaction id to be provided");

            TransactionId = transactionId;
            Cause = cause;
        }
    }

    public struct Transfer : IDomainCommand, ITransactional
    {
        public string TransactionId { get; }
        public string ToAccountNr { get; }
        public decimal Amount { get; }

        public Transfer(string transactionId,  string toAccountNr, decimal amount)
        {
            if (string.IsNullOrEmpty(transactionId)) throw new ArgumentNullException(nameof(transactionId), "Transfer command require transaction id to be provided");
            if (amount < 0M) throw new ArgumentException("Cannot make transfer of non-positive amount of money", nameof(amount));
            if (string.IsNullOrEmpty(toAccountNr)) throw new ArgumentNullException(nameof(toAccountNr), "Cannot continue transfer - receiver account nr was not provided");
            
            Amount = amount;
            TransactionId = transactionId;
            ToAccountNr = toAccountNr;
        }
    }

    public struct TransferSucceed
    {
        public readonly string TransactionId;

        public TransferSucceed(string transactionId)
        {
            TransactionId = transactionId;
        }
    }

    public struct TransferFailed
    {
        public readonly string TransactionId;
        public readonly Exception Reason;
        
        public TransferFailed(string transactionId, Exception reason)
        {
            TransactionId = transactionId;
            Reason = reason;
        }
    }
}