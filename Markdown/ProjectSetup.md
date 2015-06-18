# Project Setup
And we're back! So last time we looked at some of the basic configuration for MembershipReboot that was shown in the SingleTenant sample project. My goal is to create a library that wraps up all of the configuration and setup for easy consumption by a web project. So let's get started.

I started by creating an empty solution with a class library like so:

![alt text](Images/ProjectSetup-InitialSolution.png "Initial Solution Explorer")

Then I added the MembershipReboot NuGet packages by running the following commands:
```
Install-Package BrockAllen.MembershipReboot.Ef -DependencyVersion Highest
Install-Package BrockAllen.MembershipReboot.WebHost -DependencyVersion Highest
```

Next we will add in the basic configuration that we saw last time, but we will accept as many defaults as possible until we understand more about what is going on. We will first rename `Class1` to something more meaningful and add some properties:
```csharp
public class Membership
{
    public Membership(string connectionString)
    {
        this.Database = new DefaultMembershipRebootDatabase(connectionString);
        this.UserRepository = new DefaultUserAccountRepository(this.Database);
        this.UserService = new UserAccountService(this.UserRepository);
        this.AuthService = new SamAuthenticationService(this.UserService);
    }

    public DefaultMembershipRebootDatabase Database { get; private set; }
    public IUserAccountRepository UserRepository { get; private set; }
    public UserAccountService UserService { get; private set; }
    public AuthenticationService AuthService { get; private set; }
}
```

Ok, now if we flip back to the SingleTenant example we will find some code that shows us how to use these classes properly. First, let's look at: `Areas/UserAccount/Controllers/LoginController.cs`. Notice that in the `Index` function the first lines after checking if the model state is valid are:
```csharp
BrockAllen.MembershipReboot.UserAccount account;
if (userAccountService.AuthenticateWithUsernameOrEmail(model.Username, model.Password, out account))
{
    authSvc.SignIn(account, model.RememberMe);
    ...
```

These lines of code are what actually allow us to login and get a `UserAccount` back from the database. So let's add some code to do that:
```csharp
public bool Authenticate(string usernameOrEmail, string password)
{
    UserAccount account;
    AuthenticationFailureCode failureCode;

    bool authenticated = this.Account != null;
    if (!authenticated &&
        this.UserService.AuthenticateWithUsernameOrEmail(usernameOrEmail, password, out account, out failureCode))
    {
        this.Account = account;
        return true;
    }

    return authenticated;
}
```

Please note that I have not included the call to `SignIn` just yet. It turns out that `SignIn` requires that this be running in an ASP.NET context because it requires access to `HttpContext.Current`. I have added code to the file to allow for signing in, but I will omit that until the end. So now we can authenticate a user, but we first need the ability to create and delete accounts. So let's add the code for that like so:
```csharp
public void CreateAccount(string username, string password, string email)
{
    this.Account = this.UserService.CreateAccount(username, password, email);
}

public void DeleteAccount()
{
    if (this.Account != null)
    {
        this.UserService.DeleteAccount(this.Account.ID);
        this.Account = null;
    }
}
```

Now that we have the code, it's time to test it out. I created a unit test project that references the main Membership project. But there is a gotcha: You also need to add the EntityFramework NuGet package to the unit test project, otherwise you will get some referencing errors:

![alt text](Images/ProjectSetup-SolutionWithUnitTests.png "Added Unit Test Project")


```
Install-Package EntityFramework -DependencyVersion Highest
```

Now I'll add 3 tests to try out the functions we wrote:
```csharp
[TestClass]
public class MembershipTests
{
    private const string ConnectionString = @"Data Source=(localdb)\ProjectsV12;Initial Catalog=MembershipReboot;Integrated Security=True";

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
```

Just a quick note: I used `try/finally` because in order to make the tests repeatable we need to remove the account we just created, even if there was an exception. Maybe not the best way of doing things, but that's what we have for now. Run these tests and you should get a new database called `MembershipReboot` in your localdb at `(localdb)\\ProjectsV12`. If you set breakpoints after the calls to create, you will see entries in the `UserAccounts` table in the database:

![alt text](Images/ProjectSetup-LocalDbEntry.png "User Account Entry in LocalDB")

For a final overview of the code see [here](../Source/MRTutorial-ProjectSetup)

So that wraps up what we have for now. Future iterations of the project will be kept in seperate folders to make it easier to follow along. Now we start looking at intergration with IdentityServer. Until next time.