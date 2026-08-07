using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Predictathon.WebApi.Controllers.Base;

namespace Predictathon.WebApi.Controllers;

/// <summary>
/// Trivial reachability check for the API, independent of the database (unlike "/health") -
/// useful for confirming routing/hosting (an IIS sub-application, a reverse proxy) is wired up
/// correctly before worrying about anything else. Also reports the running version, so a manual
/// Plesk deploy can be confirmed without RDP/log-diving.
/// </summary>
[AllowAnonymous]
public class PingController : ApiControllerBase
{
    private static readonly string Version =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";

    /// <summary>
    /// Returns a status/version payload - "pong" appears in the body so existing keyword-based
    /// uptime monitors keep working unchanged.
    /// </summary>
    [HttpGet]
    public ActionResult<object> Get() => Ok(new { status = "pong", version = Version });
}
