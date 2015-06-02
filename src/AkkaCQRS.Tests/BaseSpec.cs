using Akka.TestKit.Xunit;
using AkkaCQRS.Core;

namespace AkkaCQRS.Tests
{
    public abstract class BaseSpec : TestKit
    {
        protected BaseSpec()
        {
            CqrsExtensionProvider.Instance.Apply(Sys);
        }
    }
}