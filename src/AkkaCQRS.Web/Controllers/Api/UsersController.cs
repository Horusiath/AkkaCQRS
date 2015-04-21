using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Akka.Actor;
using AkkaCQRS.Core;
using AkkaCQRS.Core.Users;
using AkkaCQRS.Web.Infrastructure.Async;
using AkkaCQRS.Web.Models.Users;

namespace AkkaCQRS.Web.Controllers.Api
{
    public class UsersController : ApplicationController
    {
        public UsersController()
        {
        }

        public async Task<UserEntity> Get(Guid id)
        {
            var users = await GetActor<UserCoordinator>();
            return await users.Ask<UserEntity>(new GetState(id), DefaultTimeout);
        }

        [HttpPost]
        public async Task<UserInfo> Post(UserCommands.RegisterUser request)
        {
            var usersIndex = await GetActor<UserIndex>();
            var emailReply = await usersIndex.Ask<UserIndex.IReply>(new UserIndex.GetUserByEmail(request.Email), DefaultTimeout);

            // cannot register user using email, which has already been used
            if (emailReply is UserIndex.UserFound)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var users = await GetActor<UserCoordinator>();
            var entity = await users.Ask<UserEntity>(request, DefaultTimeout);
            return new UserInfo(entity.Id, entity.FirstName, entity.LastName, entity.Email, entity.AccountsIds.ToArray());
        }

        [HttpPost]
        [Route("api/users/signin")]
        public async Task<UserInfo> PostSignin(UserCommands.SignInUser request)
        {
            var users = await GetActor<UserCoordinator>();
            var entity = await users.Ask<UserEntity>(request, DefaultTimeout);
            var userInfo = new UserInfo(entity.Id, entity.FirstName, entity.LastName, entity.Email, entity.AccountsIds.ToArray());
            return userInfo;
        }
    }
}