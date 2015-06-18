using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using BrockAllen.MembershipReboot;
using BrockAllen.MembershipReboot.Ef;
using BrockAllen.MembershipReboot.Relational;
using BrockAllen.MembershipReboot.WebHost;

namespace MRTutorial.Membership
{
    public class Membership
    {
        public Membership(string connectionString)
        {
            this.Database = new DefaultMembershipRebootDatabase(connectionString);
            this.UserRepository = new DefaultUserAccountRepository(this.Database);
            this.UserService = new UserAccountService(this.UserRepository);
            this.AuthService = new SamAuthenticationService(this.UserService);

            this.UserService.Configuration.RequireAccountVerification = false;
        }

        public UserAccount Account { get; private set; }
        public DefaultMembershipRebootDatabase Database { get; private set; }
        public IUserAccountRepository UserRepository { get; private set; }
        public UserAccountService UserService { get; private set; }
        public AuthenticationService AuthService { get; private set; }


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

        public void SignIn(string usernameOrEmail, string password, bool rememberMe = false)
        {
            UserAccount account;
            AuthenticationFailureCode failureCode;

            bool authenticated = this.Authenticate(usernameOrEmail, password);
            if (authenticated)
            {
                this.AuthService.SignIn(this.Account, rememberMe);

                if (this.Account.RequiresTwoFactorAuthCodeToSignIn())
                {
                    throw new NotImplementedException();
                }
                if (this.Account.RequiresTwoFactorCertificateToSignIn())
                {
                    throw new NotImplementedException();
                }

                if (this.Account.RequiresPasswordReset)
                {
                    // this might mean many things -- 
                    // it might just mean that the user should change the password, 
                    // like the expired password below, so we'd just redirect to change password page
                    // or, it might mean the DB was compromised, so we want to force the user
                    // to reset their password but via a email token, so we'd want to 
                    // let the user know this and invoke ResetPassword and not log them in
                    // until the password has been changed
                    //userAccountService.ResetPassword(account.ID);

                    // so what you do here depends on your app and how you want to define the semantics
                    // of the RequiresPasswordReset property
                    throw new NotImplementedException();
                }

                if (this.UserService.IsPasswordExpired(this.Account))
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                throw new AuthenticationException("Authentication failed.");
            }
        }
    }
}
