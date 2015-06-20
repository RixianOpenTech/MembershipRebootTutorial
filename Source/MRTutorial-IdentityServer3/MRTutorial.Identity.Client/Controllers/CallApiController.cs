using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.UI.WebControls;
using Newtonsoft.Json.Linq;
using Thinktecture.IdentityModel.Client;

namespace MRTutorial.Identity.Client.Controllers
{
    [RoutePrefix("callapi")]
    public class CallApiController : ApiController
    {    
        // GET: CallApi/ClientCredentials
        [HttpGet]
        public async Task<IHttpActionResult> ClientCredentials()
        {
            var response = await GetTokenAsync();
            var result = await CallApi(response.AccessToken);

            return Json(result);
        }

        private async Task<TokenResponse> GetTokenAsync()
        {
            var client = new OAuth2Client(
                new Uri("https://localhost:44301/identity/connect/token"),
                "client",
                "secret");

            return await client.RequestClientCredentialsAsync("sampleApi");
        }

        private async Task<string> CallApi(string token)
        {
            var client = new HttpClient();
            client.SetBearerToken(token);

            var json = await client.GetStringAsync("https://localhost:44301/test");
            return JArray.Parse(json).ToString();
        }
    }
}
