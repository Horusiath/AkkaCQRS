using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Akka.Actor;
using AkkaCQRS.Core.Accounting;
using AkkaCQRS.Web.Models.Accounts;

namespace AkkaCQRS.Web.Controllers.Api
{
    public class AccountsController : ApplicationController
    {
        [HttpPost]
        public async Task<object> Post(CreateAccount request)
        {
            if (ModelState.IsValid)
            {
                var accounts = await GetActor<AccountCoordinator>();
                var accountId = Guid.NewGuid();
                var entity = await accounts.Ask<AccountEntity>(new AccountCommands.CreateAccount(accountId, request.OwnerId, 0.0M));

                return entity;
            }
            
            throw new HttpResponseException(HttpStatusCode.BadRequest);
        }

        [HttpPatch]
        [Route("api/accounts/{accountId}/deposit")]
        public async Task PatchDeposit(Deposit request)
        {
            if (ModelState.IsValid)
            {
                var accounts = await GetActor<AccountCoordinator>();
                accounts.Tell(new AccountCommands.Deposit(request.AccountId, request.Amount), ActorRefs.NoSender);
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
        }

        [HttpPatch]
        [Route("api/accounts/{accountId}/withdraw")]
        public async Task PatchWithdrawal(Withdraw request)
        {
            if (ModelState.IsValid)
            {
                var accounts = await GetActor<AccountCoordinator>();
                accounts.Tell(new AccountCommands.Withdraw(request.AccountId, request.Amount), ActorRefs.NoSender);
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
        }

        [HttpPatch]
        [Route("api/accounts/{accountId}/transfer")]
        public async Task<object> PatchTransfer(Transfer request)
        {
            if (ModelState.IsValid)
            {
                //TODO
                throw new NotImplementedException();
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
        }

        [HttpDelete]
        public async Task DeleteAccount(Deactivate request)
        {
            if (ModelState.IsValid)
            {
                var accounts = await GetActor<AccountCoordinator>();
                accounts.Tell(new AccountCommands.DeactivateAccount(request.AccountId), ActorRefs.NoSender);
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
        }
    }
}
