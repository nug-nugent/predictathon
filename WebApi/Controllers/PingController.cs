using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

/// <summary>
/// Trivial reachability check for the API, independent of the database (unlike "/health") -
/// useful for confirming routing/hosting (an IIS sub-application, a reverse proxy) is wired up
/// correctly before worrying about anything else.
/// </summary>
[AllowAnonymous]
public class PingController : ApiControllerBase
{
    /// <summary>
    /// Returns "pong" - confirms the request reached this API instance.
    /// </summary>
    [HttpGet]
    public ActionResult<string> Get() => Ok("pong");
}
