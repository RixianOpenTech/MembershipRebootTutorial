using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BrockAllen.MembershipReboot;
using Microsoft.Owin;
using Owin;
using Thinktecture.IdentityServer.Core.Configuration;
using Thinktecture.IdentityServer.Core.Models;
using Thinktecture.IdentityServer.Core.Services;
using Thinktecture.IdentityServer.EntityFramework;
using Thinktecture.IdentityServer.MembershipReboot;

[assembly: OwinStartup(typeof(MRTutorial.Identity.Startup))]

namespace MRTutorial.Identity
{
    public class Startup
    {
        private const string MembershipConnectionString =
            @"Data Source=(localdb)\ProjectsV12;Initial Catalog=MembershipDB;Integrated Security=True";
        private const string IdentityConnectionString =
            @"Data Source=(localdb)\ProjectsV12;Initial Catalog=IdentityDB;Integrated Security=True";
        //https://localhost:44301/identity/.well-known/openid-configuration

        public void Configuration(IAppBuilder app)
        {
            Membership.Membership membership = new Membership.Membership(MembershipConnectionString);
            MembershipRebootUserService<UserAccount> userService = new MembershipRebootUserService<UserAccount>(membership.UserService);
            
            var efConfig = new EntityFrameworkServiceOptions
            {
                ConnectionString = IdentityConnectionString,
                //Schema = "someSchemaIfDesired"
            };

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

        X509Certificate2 LoadCertificate()
        {
            return new X509Certificate2(
                string.Format(@"{0}\bin\idsrv3test.pfx", AppDomain.CurrentDomain.BaseDirectory), "idsrv3test");
        }
    }
}
