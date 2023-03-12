using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Light.Web;

[Authorize]
[ApiController]
[Route("api/[controller]/[action]")]
public class ApiController : ControllerBase
{
    [NonAction]
    public virtual OkObjectResult Ok<T>(long total, IEnumerable<T> list) =>
        new(new { Total = total, List = list });
}