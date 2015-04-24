using System;

namespace AkkaCQRS.Core
{
    public interface IMessage { }

    public interface IAddressed
    {
        Guid RecipientId { get; }
    }

    public interface ICommand : IMessage { }

    public interface IEvent : IMessage { }

    public sealed class GetState : ICommand
    {
        public readonly Guid Id;

        public GetState(Guid id)
        {
            Id = id;
        }
    }

    public sealed class UnauthorizedRequest<TReq> : IMessage
    {
        public readonly TReq Request;

        public UnauthorizedRequest(TReq request)
        {
            Request = request;
        }
    }

    public static class Unauthorized
    {
        public static UnauthorizedRequest<TReq> Message<TReq>(TReq message) where TReq : IMessage
        {
            return new UnauthorizedRequest<TReq>(message);
        }
    }
}