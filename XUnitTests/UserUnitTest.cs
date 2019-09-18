using System;
using Xunit;

namespace XUnitTests
{
    public class UserUnitTest
    {
        [Fact]
        public async void GetMeTest()
        {
            var client = HarvestClientFactory.Create();
            var me = await client.GetMe();

            Assert.NotNull(me);
        }

        [Fact]
        public async void GetUsersTest()
        {
            var client = HarvestClientFactory.Create();
            var users = await client.GetUsers();

            Assert.NotNull(users.Users);
            Assert.NotEmpty(users.Users);
        }
    }
}
