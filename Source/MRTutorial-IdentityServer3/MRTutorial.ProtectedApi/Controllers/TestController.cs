using System.Linq;
using System.Security.Claims;
using System.Web.Http;

namespace MRTutorial.ProtectedApi.Controllers
{
    [Authorize]
    [RoutePrefix("test")]
    public class TestController : ApiController
    {
        [HttpGet]
        public IHttpActionResult Get()
        {
            var user = User as ClaimsPrincipal;
            var claims = from c in user.Claims
                         select new
                         {
                             type = c.Type,
                             value = c.Value
                         };

            return Json(claims);
        }
    }
}
