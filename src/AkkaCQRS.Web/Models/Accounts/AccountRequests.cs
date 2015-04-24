using System;
using System.ComponentModel.DataAnnotations;

namespace AkkaCQRS.Web.Models.Accounts
{
    [Serializable]
    public sealed class CreateAccount
    {
        [Required]
        public Guid OwnerId { get; set; }
    }

    [Serializable]
    public sealed class Deposit
    {
        [Required]
        public Guid AccountId { get; set; }
        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    [Serializable]
    public sealed class Withdraw
    {
        [Required]
        public Guid AccountId { get; set; }
        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    [Serializable]
    public sealed class Transfer
    {
        [Required]
        public Guid FormAccountId { get; set; }
        [Required]
        public Guid ToAccountId { get; set; }
        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    [Serializable]
    public sealed class Deactivate
    {
        [Required]
        public Guid AccountId { get; set; }
    }
}