# IdentityServer3

Alright, we are back and looking at IdentityServer3. I assume you already know what it is and how it basically works. Also before we begin please read through this article in their documentation, as we will start with this:
http://identityserver.github.io/Documentation/docs/overview/mvcGettingStarted.html

And... we're back. So we are going to take our MembershipReboot code from last time and add a WebApi project to become our IdentityServer. Let's add it and reference the following NuGet packages:

```
install-package Microsoft.Owin.Host.Systemweb -DependencyVersion Highest
install-package Thinktecture.IdentityServer3 -DependencyVersion Highest
install-package Microsoft.Owin.Security.Cookies -DependencyVersion Highest
install-package Microsoft.Owin.Security.OpenIdConnect -DependencyVersion Highest
install-package Microsoft.AspNet.WebApi -DependencyVersion Highest
install-package Thinktecture.IdentityServer3.EntityFramework -DependencyVersion Highest
install-package Thinktecture.IdentityServer3.MembershipReboot -DependencyVersion Highest
```

Also add a reference to the Membership project we created last time. Make sure to go download a test certificate for us to use and include it into the project:
https://github.com/IdentityServer/IdentityServer3.Samples/tree/master/source/Certificates

One last super important thing is to add the following to the web.config file so the the IdentySerrer resources will load correctly:
```xml
<system.webServer>
	<modules runAllManagedModulesForAllRequests="true">
	</modules>
</system.webServer>
```

Next create an OWIN Startup class and add the following code that we will review in a minute:
```csharp
public class Startup
{
	private const string MembershipConnectionString =
	    @"Data Source=(localdb)\ProjectsV12;Initial Catalog=MembershipDB;Integrated Security=True";
	private const string IdentityConnectionString =
	    @"Data Source=(localdb)\ProjectsV12;Initial Catalog=IdentityDB;Integrated Security=True";
	
	public void Configuration(IAppBuilder app)
	{
	    Membership.Membership membership = new Membership.Membership(MembershipConnectionString);
	    MembershipRebootUserService<UserAccount> userService = new MembershipRebootUserService<UserAccount>(membership.UserService);
	    
	    var efConfig = new EntityFrameworkServiceOptions
	    {
	        ConnectionString = IdentityConnectionString,
	        //Schema = "someSchemaIfDesired"
	    };
	
	    ConfigureClients(Clients.Get(), efConfig);
	
	    var factory = new IdentityServerServiceFactory();
	    factory.UserService = new Registration<IUserService>(userService); 
	    factory.RegisterConfigurationServices(efConfig);
	    factory.RegisterOperationalServices(efConfig);
	    var cleanup = new TokenCleanup(efConfig, 10);
	    cleanup.Start();
	
	    var options = new IdentityServerOptions
	    {
	        SiteName = "Embedded IdentityServer",
	        SigningCertificate = LoadCertificate(),
	        Factory = factory
	    };
	
	    app.Map("/identity", idsrvApp => idsrvApp.UseIdentityServer(options));
	}
	
	public static void ConfigureClients(IEnumerable<Client> clients, EntityFrameworkServiceOptions options)
	{
	    using (var db = new ClientConfigurationDbContext(options.ConnectionString, options.Schema))
	    {
	        if (!db.Clients.Any())
	        {
	            foreach (var c in clients)
	            {
	                var e = c.ToEntity();
	                db.Clients.Add(e);
	            }
	            db.SaveChanges();
	        }
	    }
	}
	
	X509Certificate2 LoadCertificate()
	{
	    return new X509Certificate2(
	        string.Format(@"{0}\bin\idsrv3test.pfx", AppDomain.CurrentDomain.BaseDirectory), "idsrv3test");
	}
}
```

And to finish off add a new class called Clients with the following code:
```csharp
public class Clients
{
    public static List<Client> Get()
    {
        return new List<Client>
        {
            new Client
            {
                ClientName = "Code Flow Clients",
                Enabled = true,
                ClientId = "codeclient",
                ClientSecrets = new List<ClientSecret>{
                    new ClientSecret("secret".Sha256())
                },
                Flow = Flows.AuthorizationCode,
                
                RequireConsent = true,
                AllowRememberConsent = true,
                
                ClientUri = "http://www.thinktecture.com",
                RedirectUris = new List<string>
                {
                    // MVC code client with module
                    "https://localhost:44320/oidccallback",
                    
                    // MVC code client manual
                    "https://localhost:44312/callback"
                },
                
                ScopeRestrictions = new List<string>
                { 
                    Constants.StandardScopes.OpenId,
                    Constants.StandardScopes.Profile,
                    Constants.StandardScopes.Email,
                    Constants.StandardScopes.OfflineAccess,
                    "read",
                    "write"
                },

                //SubjectType = SubjectTypes.Global,
                AccessTokenType = AccessTokenType.Reference,
                
                IdentityTokenLifetime = 360,
                AccessTokenLifetime = 360,
                AuthorizationCodeLifetime = 120
            },

            new Client
            {
                ClientName = "Implicit Clients",
                Enabled = true,
                ClientId = "implicitclient",
                ClientSecrets = new List<ClientSecret>{
                    new ClientSecret("secret".Sha256())
                },
                Flow = Flows.Implicit,
                
                ClientUri = "http://www.thinktecture.com",
                RequireConsent = true,
                AllowRememberConsent = true,
                
                RedirectUris = new List<string>
                {
                    // WPF client
                    "oob://localhost/wpfclient",
                    
                    // JavaScript client
                    "http://localhost:21575/index.html",

                    // MVC form post sample
                    "http://localhost:11716/account/signInCallback",

                    // OWIN middleware client
                    "http://localhost:2671/",
                },
                
                ScopeRestrictions = new List<string>
                { 
                    Constants.StandardScopes.OpenId,
                    Constants.StandardScopes.Profile,
                    Constants.StandardScopes.Email,
                    "read",
                    "write"
                },

                //SubjectType = SubjectTypes.Global,
                AccessTokenType = AccessTokenType.Jwt,
                
                IdentityTokenLifetime = 360,
                AccessTokenLifetime = 360,
            },
            new Client
            {
                ClientName = "Client Credentials Flow Client",
                Enabled = true,
                ClientId = "client",
                Flow = Flows.ClientCredentials,
                
                ScopeRestrictions = new List<string>
                { 
                    "read",
                    "write"
                },

                AccessTokenType = AccessTokenType.Jwt,
                AccessTokenLifetime = 360,
            },
            new Client
            {
                ClientName = "Resource Owner Flow Client",
                Enabled = true,
                ClientId = "roclient",
                ClientSecrets = new List<ClientSecret>{
                    new ClientSecret("secret".Sha256())
                },
                Flow = Flows.ResourceOwner,
                
                ScopeRestrictions = new List<string>
                { 
                    Constants.StandardScopes.OfflineAccess,
                    "read",
                    "write"
                },

                AccessTokenType = AccessTokenType.Jwt,
                AccessTokenLifetime = 360,
            }
        };
    }
}
```  

## Code Review
Now let us get an understanding of what is going on here. First we'll look at the code for the `Startup.cs` file. First of all note that we have connection strings to two different databases, one for the MembershipReboot User database, and one for the IdentityServer3 database:
```csharp
private const string MembershipConnectionString =
    @"Data Source=(localdb)\ProjectsV12;Initial Catalog=MembershipDB;Integrated Security=True";
private const string IdentityConnectionString =
    @"Data Source=(localdb)\ProjectsV12;Initial Catalog=IdentityDB;Integrated Security=True";
```

MembershipReboot will keep track of everything user-related. So groups, linked accounts, passwords, etc... The IdentityServer3 database keeps track of client applications and the permissions that each application and user have. It handles tokens and authentication in general. Remember the difference between those two and we will be fine. 

So for simplicity we will be using EntityFramework for the data access, even though I highly advise against it. So in the Configuration function we start off by setting up MembershipReboot:
```csharp
Membership.Membership membership = new Membership.Membership(MembershipConnectionString);
MembershipRebootUserService<UserAccount> userService = new MembershipRebootUserService<UserAccount>(membership.UserService);
```
The first line uses the membership code we wrote last time, and the second line uses the IdentityServer-MembershipReboot connector thingy. It's basically the bridge between IdentityServer and MembershipReboot. IdentityServer3 has various services that can be plugged and replaced, and this adapter replaces the `IUserService` implementation.

So with that done we set up the IdentityServer3 database with the following lines of code:
```csharp
var efConfig = new EntityFrameworkServiceOptions
{
    ConnectionString = IdentityConnectionString,
    //Schema = "someSchemaIfDesired"
};

ConfigureClients(Clients.Get(), efConfig);

var factory = new IdentityServerServiceFactory();
factory.UserService = new Registration<IUserService>(userService); 
factory.RegisterConfigurationServices(efConfig);
factory.RegisterOperationalServices(efConfig);
var cleanup = new TokenCleanup(efConfig, 10);
cleanup.Start();
```

The `efConfig` simply configures the connection to EntityFramework (just the connection string in this case). The `ConfigureClients` call is required for a couple of reasons. Most importantly, it must be called so that EF will create the correct tables in the database. That happens when the function calls `new ClientConfigurationDbContext`. It also pre-populates the tables with some data that we will use later on.

So the next three lines will all replace the most important services with EF implementations:
```csharp
factory.UserService = new Registration<IUserService>(userService); 
factory.RegisterConfigurationServices(efConfig);
factory.RegisterOperationalServices(efConfig);
```

First we replace the `IUserService` with the MembershipReboot implementation. Then we replace the Configuration and Operational services. Specifically the call to `factory.RegisterConfigurationServices(efConfig)` is a wrapper around these two calls:
```csharp
factory.RegisterClientStore(efConfig);
factory.RegisterScopeStore(efConfig);
```
(see: http://identityserver.github.io/Documentation/docs/ef/clients_scopes.html)

The call to `factory.RegisterOperationalServices(efConfig)` replaces all the services for "authorization codes, refresh tokens, reference tokens, and user consent".

(see: http://identityserver.github.io/Documentation/docs/ef/operational.html)

Finally the following code simply polls the database attempting to clean up expired tokens and such:
```csharp
var cleanup = new TokenCleanup(efConfig, 10);
cleanup.Start();
```

The last few lines set up IdentityServer using the test certificate and the factory that we just configured.


![alt text](Images/ProjectSetup-LocalDbEntry.PNG "User Account Entry in LocalDB")