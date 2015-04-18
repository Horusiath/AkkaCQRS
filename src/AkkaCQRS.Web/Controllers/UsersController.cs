using System;
using System.Threading.Tasks;
using Akka.Actor;
using AkkaCQRS.Core;
using AkkaCQRS.Core.Users;

namespace AkkaCQRS.Web.Controllers
{
    public class UsersController : ApplicationController
    {
        private readonly ICanTell _users; 

        public UsersController()
        {
            var addressBook = System.GetAddressBook();

            _users = addressBook.Ask<AddressBook.Found>(new AddressBook.Get(typeof (UserCoordinator))).Result.Ref;
        }

        public async Task<UserEntity> Get(Guid id)
        {
            return await _users.Ask<UserEntity>(new GetState(id), DefaultTimeout);
        }

        public async Task<UserEntity> Post(UserCommands.RegisterUser request)
        {
            return await _users.Ask<UserEntity>(request, DefaultTimeout);
        }
    }
}