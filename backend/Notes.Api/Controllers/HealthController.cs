using Microsoft.AspNetCore.Mvc;
using Notes.Api.Dtos;

namespace Notes.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse("ok"));
}
