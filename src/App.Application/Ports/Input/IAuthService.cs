using App.Application.DTOs;

namespace App.Application.Ports.Input;

/// <summary>
/// Porta de entrada (driving port): contrato que os adaptadores externos (ex.: WebApi, CLI, testes)
/// usam para registrar e autenticar usuários. Implementada por AuthService.
/// </summary>
public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
