using System;
using Akka.Actor;
using Akka.TestKit;
using AkkaCQRS.Core;
using AkkaCQRS.Core.Users;
using Xunit;

namespace AkkaCQRS.Tests.Users
{
    public class UserSpec : AggregateRootSpec
    {
        private const string fname = "john";
        private const string lname = "doe";
        private const string email = "john.doe@fakemail.op";
        private const string password = "password";

        private Guid _userId;
        private IActorRef _userRef;
        private TestProbe _eventListener;

        public UserSpec()
        {
            _userId = Guid.NewGuid();
            _userRef = ActorOf(() => new User(_userId));
            _eventListener = CreateTestProbe();
        }

        #region Uninitialized state

        [Fact]
        public void Uninitialized_User_should_respond_on_RegisterUser_command_and_hash_password()
        {
            Subscribe<UserEvents.UserRegistered>(_eventListener);

            _userRef.Tell(new UserCommands.RegisterUser(fname, lname, email, password));
            
            _eventListener.ExpectMsg<UserEvents.UserRegistered>(e =>
                e.Id == _userId &&
                e.FirstName == fname &&
                e.LastName == lname &&
                e.PasswordHash != password);

            ExpectMsg<UserEntity>(entity => 
                entity.Id == _userId && 
                entity.FirstName == fname && 
                entity.LastName == lname && 
                entity.Email == email && 
                entity.PasswordHash != password);
        }
        
        #endregion

        #region Initialized state

        [Fact]
        public void Initialized_User_should_be_able_to_sign_in_himself_using_correct_credentials()
        {
            Subscribe<UserEvents.UserSignedIn>(_eventListener);
            _userRef.Tell(new UserCommands.RegisterUser(fname, lname, email, password));
            
            _userRef.Tell(new UserCommands.SignInUser(email, password));

            _eventListener.ExpectMsg<UserEvents.UserSignedIn>(e => e.Id == _userId);
        }

        [Fact]
        public void Initialized_User_should_not_be_able_to_sign_in_himself_using_invalid_credentials()
        {
            _userRef.Tell(new UserCommands.RegisterUser(fname, lname, email, password));
            ExpectMsg<UserEntity>();

            Subscribe<UserEvents.UserSignedIn>(_eventListener);
            _userRef.Tell(new UserCommands.SignInUser(email, "fakepassword"));
            _eventListener.ExpectNoMsg();

            ExpectMsg<UnauthorizedRequest<UserCommands.SignInUser>>(msg => msg.Request.Email == email);
        }

        #endregion

        #region Signed up state

        [Fact]
        public void SignedIn_User_should_be_able_to_change_his_password_using_valid_old_password()
        {
            _userRef.Tell(new UserCommands.RegisterUser(fname, lname, email, password));
            _userRef.Tell(new UserCommands.SignInUser(email, password));
            
            Subscribe<UserEvents.PasswordChanged>(_eventListener);
            _userRef.Tell(new UserCommands.ChangePassword(_userId, password, "newpassword"));
            _eventListener.ExpectMsg<UserEvents.PasswordChanged>(e => e.Id == _userId);
        }

        [Fact]
        public void SignedIn_User_should_not_be_able_to_change_his_password_using_invalid_old_password()
        {
            _userRef.Tell(new UserCommands.RegisterUser(fname, lname, email, password));
            _userRef.Tell(new UserCommands.SignInUser(email, password));

            Subscribe<UserEvents.PasswordChanged>(_eventListener);
            _userRef.Tell(new UserCommands.ChangePassword(_userId, "fakepassword", "newpassword"));
            _eventListener.ExpectNoMsg();

            ExpectMsg<UserEntity>();
            ExpectMsg<UserEntity>();
            ExpectMsg<UnauthorizedRequest<UserCommands.ChangePassword>>(msg => msg.Request.RecipientId == _userId);
        }

        #endregion

        [Fact]
        public void Initialized_User_should_remain_in_Initialized_behavior_upon_recovery()
        {
            // check if user starts with Uninitialized behavior
            _userRef.Tell(new UserCommands.RegisterUser(fname, lname, email, password), TestActor);
            ExpectMsg<UserEntity>(u => u.Id == _userId && !u.IsSigned);

            // now user is Initialized, lets kill it...
            Watch(_userRef);
            _userRef.Tell(PoisonPill.Instance);
            ExpectTerminated(_userRef);

            // ...and try to resurect
            _userRef = ActorOf(() => new User(_userId));

            // try to sign in user - this message is responded only in Initialized behavior
            _userRef.Tell(new UserCommands.SignInUser(email, password), TestActor);
            ExpectMsg<UserEntity>(u => u.Id == _userId && u.IsSigned);
        }

        [Fact]
        public void Signed_User_should_remain_in_Signed_behavior_upon_recovery()
        {
            _userRef.Tell(new UserCommands.RegisterUser(fname, lname, email, password), TestActor);
            ExpectMsg<UserEntity>(u => u.Id == _userId && !u.IsSigned);
            _userRef.Tell(new UserCommands.SignInUser(email, password), TestActor);
            ExpectMsg<UserEntity>(u => u.Id == _userId && u.IsSigned);

            // right now user is in SignedIn behavior, kill it and resurect
            Watch(_userRef);
            _userRef.Tell(PoisonPill.Instance);
            ExpectTerminated(_userRef);

            _userRef = ActorOf(() => new User(_userId));
            // check user state
            _userRef.Tell(new GetState(_userId), TestActor);
            ExpectMsg<UserEntity>(u => u.IsSigned && u.Id == _userId);

            // try to sign out user and verify it's state - only SignedIn behavior alters it
            _userRef.Tell(new UserCommands.SignOutUser(_userId), TestActor);
            // check user state
            _userRef.Tell(new GetState(_userId), TestActor);
            ExpectMsg<UserEntity>(u => !u.IsSigned && u.Id == _userId);
        }

        private void Subscribe<T>(IActorRef subscriber)
        {
            Sys.EventStream.Subscribe(subscriber, typeof (T));
        }
    }
}