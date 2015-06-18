using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MRTutorial.Membership.Tests
{
    [TestClass]
    public class MembershipTests
    {
        private const string ConnectionString =
            @"Data Source=(localdb)\ProjectsV12;Initial Catalog=MembershipReboot;Integrated Security=True";

        [TestMethod]
        public void CreateDeleteAccount_Valid_Success()
        {
            Membership membership = new Membership(ConnectionString);
            try
            {
                membership.CreateAccount("foo", "bar", "foo@bar.com");
                Assert.IsNotNull(membership.Account);
            }
            finally
            {
                membership.DeleteAccount();
                Assert.IsNull(membership.Account);
            }
        }

        [TestMethod]
        public void Authentication_Valid_Success()
        {
            Membership membership = new Membership(ConnectionString);
            try
            {
                membership.CreateAccount("foo", "bar", "foo@bar.com");
                Assert.IsNotNull(membership.Account);
                var authenticated = membership.Authenticate("foo", "bar");
                Assert.IsTrue(authenticated);
            }
            finally
            {
                membership.DeleteAccount();
                Assert.IsNull(membership.Account);
            }
        }

        [TestMethod]
        public void Authentication_NoAccount_Success()
        {
            Membership membership = new Membership(ConnectionString);
            try
            {
                membership.CreateAccount("foo", "bar", "foo@bar.com");
                Assert.IsNotNull(membership.Account);
                membership = new Membership(ConnectionString);
                Assert.IsNull(membership.Account);
                var authenticated = membership.Authenticate("foo", "bar");
                Assert.IsTrue(authenticated);
            }
            finally
            {
                membership.DeleteAccount();
                Assert.IsNull(membership.Account);
            }
        }
    }
}
