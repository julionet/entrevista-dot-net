using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.WebApi.Controllers;

/// <summary>
/// Adaptador driving (primário): endpoint de liveness, usado por monitoramento/orquestradores
/// para verificar se a API está no ar. Não depende de nenhuma outra camada.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "Healthy", timestampUtc = DateTime.UtcNow });
    }
}
