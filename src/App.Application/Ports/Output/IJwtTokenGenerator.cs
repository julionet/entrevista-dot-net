using App.Domain.Entities;

namespace App.Application.Ports.Output;

/// <summary>
/// Porta de saída (driven port): contrato que a Application exige da infraestrutura
/// para gerar e validar os tokens de autenticação de um usuário, sem conhecer o formato/algoritmo usado.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);

    /// <summary>
    /// Valida um refresh token e devolve o id do usuário associado, ou null se o token
    /// for inválido, expirado, ou não for um refresh token.
    /// </summary>
    Guid? ValidateRefreshToken(string refreshToken);
}
