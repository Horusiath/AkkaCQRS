using Xunit;

namespace AkkaCQRS.Tests.Users
{
    public class UserSpec : AggregateRootSpec
    {
        public UserSpec()
        {
        }

        [Fact]
        public void User_should_instantiate_new_user_entity_on_RegisterUser_when_in_unitinialized_state()
        {
            
        }

        [Fact]
        public void User_should_become_initialized_after_UserRegistered_event()
        {
            
        }
    }
}