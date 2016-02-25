using System;
using Akka.Actor;

namespace Akkme.Shared.Infrastructure.Utils
{
    public static class NavigationExtensions
    {
        public static string AggregateId(this IActorRef aref)
        {
            var path = aref.Path;
            return path.Name;
        }
    }
}