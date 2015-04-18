using Xunit;

namespace AkkaCQRS.Tests.Users
{
    public class UserCoordinatorSpec : AggregateCoordinatorSpec
    {
        public UserCoordinatorSpec()
        {
        }

        [Fact]
        public void UserCoordinator_should_create_user_with_new_id_and_forward_message_on_RegisterUser_command()
        {
            
        }

        [Fact]
        public void UserCoordinator_should_pass_all_addressed_user_commands_to_managed_aggregates()
        {
            
        }
    }
}