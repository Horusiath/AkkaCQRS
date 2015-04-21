using System;
using System.Threading.Tasks;
using System.Web.Http;
using Akka.Actor;
using AkkaCQRS.Core;

namespace AkkaCQRS.Web.Controllers.Api
{
    public abstract class ApplicationController : ApiController
    {
        public static readonly ActorSystem System = Bootstrap.System;
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        private readonly ICanTell _addressBook;

        protected ApplicationController()
        {
            _addressBook = System.GetAddressBook();
        }

        protected async Task<ICanTell> GetActor<T>() where T : ActorBase
        {
            var result = await _addressBook.Ask<AddressBook.Found>(new AddressBook.Get(typeof (T)));
            return result.Ref;
        }
    }
}