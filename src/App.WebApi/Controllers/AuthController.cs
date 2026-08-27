using App.Application.DTOs;
using App.Application.Ports.Input;
using App.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.WebApi.Controllers;

/// <summary>
/// Adaptador driving (primário): traduz requisições HTTP em chamadas à porta de entrada IAuthService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Register), new { id = result.Id }, result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RefreshAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
