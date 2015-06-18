# Overview

Ok, so I have been having a lot of trouble understanding how MembershipReboot works. I feel like I understand how the whole OAuth2 workflow is supposed to wrok, but the configuration, setup, and sheer number of options in MembershipReboot are confusing to me. So here I am attempting to understand for myself and for others how all this works. So here we go.

## SingleTenant
I started by looking at the SingleTenant sample project to see what useful information I can glean. The very first thing you see are several projects:

![alt text](Images/Overview-SE.png "Initial Solution Explorer")

It seems that the first 4 projects are part of the MembershipReboot infrastructure and can be ignored. So looking at the WebApp project there are several places that we can start looking. First of all I opened up Nuget to see what packages are in the project:

![alt text](Images/Overview-Nuget.png "Nuget Packages") 

Important things to note:
* Ninject is not part of MembershipReboot, nor is it required.
* WebActivatorEx is simply to facilitate Ninject on startup

Now, RouteConfig.cs and FilterConfig.cs seem to be unchanged so we don't need to look at them. Opening NinjectWebCommon.cs we see a bunch of code that is specific to Ninject and can ignore. What is immediatly important is the last function, RegisterServices:

```csharp
private static void RegisterServices(IKernel kernel)
{
    Database.SetInitializer(new MigrateDatabaseToLatestVersion<DefaultMembershipRebootDatabase, BrockAllen.MembershipReboot.Ef.Migrations.Configuration>());

    var config = MembershipRebootConfig.Create();
    kernel.Bind<MembershipRebootConfiguration>().ToConstant(config);
    kernel.Bind<DefaultMembershipRebootDatabase>().ToSelf();
    kernel.Bind<UserAccountService>().ToSelf();
    kernel.Bind<AuthenticationService>().To<SamAuthenticationService>();
    kernel.Bind<IUserAccountQuery>().To<DefaultUserAccountRepository>().InRequestScope();
    kernel.Bind<IUserAccountRepository>().To<DefaultUserAccountRepository>().InRequestScope();
} 
```
For now let's ignore the database initializer and look at MembershipReboot. Right after the database initializer we see a line that call into another custom class `MembershipRebootConfig` which is supposed to create in instance of the `MembershipRebootConfiguration` class. Be careful of the naming similarities. 

The class we are interested in looks like this:
```csharp
public class MembershipRebootConfig
{
    public static MembershipRebootConfiguration Create()
    {
        var config = new MembershipRebootConfiguration();
        //config.RequireAccountVerification = false;
        config.AddEventHandler(new DebuggerEventHandler());

        var appinfo = new AspNetApplicationInformation("Test", "Test Email Signature",
            "UserAccount/Login", 
            "UserAccount/ChangeEmail/Confirm/",
            "UserAccount/Register/Cancel/",
            "UserAccount/PasswordReset/Confirm/");
        var emailFormatter = new EmailMessageFormatter(appinfo);
        // uncomment if you want email notifications -- also update smtp settings in web.config
        config.AddEventHandler(new EmailAccountEventsHandler(emailFormatter));

        // uncomment to enable SMS notifications -- also update TwilloSmsEventHandler class below
        //config.AddEventHandler(new TwilloSmsEventHandler(appinfo));
        
        // uncomment to ensure proper password complexity
        //config.ConfigurePasswordComplexity();

        var debugging = false;
#if DEBUG
        debugging = true;
#endif
        // this config enables cookies to be issued once user logs in with mobile code
        config.ConfigureTwoFactorAuthenticationCookies(debugging);

        return config;
    }
}
```

Let's go through this line by line so that I can get a good understanding of what is going on. The first line is:
```csharp
var config = new MembershipRebootConfiguration();
```
Notice that if you follow the reference we find that `MemebershipRebootConfiguration` is actually a sub-class of the generic `MemebershipRebootConfiguration<TAccount>` type. Specifically it inherits from `MembershipRebootConfiguration<UserAccount>`. I will gloss over this for now, but I think it's important to note that you can do 4 things to set up the configuration:
* Do nothing and accept the pre-defined default values
* Do nothing in code, but rather define the configuration options in a config file
* Pass an instance of `SecuritySettings` to the constructor
* Do one of the previous three options and make changes in code after creation

So jumping back to the code, the next commented-out line seems to make changes to the configuration by overiding what was defined in the config file:
```csharp
//config.RequireAccountVerification = false;
```
 
The following line seems to add an event listener that simply writes out the events to the debug output:
```csharp
config.AddEventHandler(new DebuggerEventHandler());
```

The next line sets the paths to each url that MembershipReboot needs, most importantly:
* `relativeLoginUrl`
* `relativeConfirmChangeEmailUrl`
* `relativeCancelVerificationUrl`
* `relativeConfirmPasswordResetUrl`

The rest of the method looks like we can skip it easily, but I want to take a look at this line:
```csharp
config.ConfigureTwoFactorAuthenticationCookies(debugging);
```

The comment says:
> this config enables cookies to be issued once user logs in with mobile code
Whatever that means...


Now we jump back up to the `NinjectWebCommon.cs` file. Once the configuration has been created we then move onto Ninject code. Note that Ninject is NOT required. It's simply a DI framework, and so feel free to either toss it or use your favorite framework. Note the way it is structured:
`kernel.Bind<T>()` simply means that whenever someone requests type T from the kernel, we are goinf to return something of that type. So the line:
`kernel.Bind<MembershipRebootConfiguration>().ToConstant(config);` says that when someone requests an instance of `MembershipRebootConfiguration` we are going to return the constant value `config`.

All of the versions that return `.ToSelf();` will simply return an instance of that class, and `.To<TSomething>();` defines a concrete type to use.
 