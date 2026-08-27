using App.Domain.Entities;

namespace App.Application.Ports.Output;

/// <summary>
/// Porta de saída (driven port): contrato que a Application exige da infraestrutura
/// para gerar o token de autenticação de um usuário, sem conhecer o formato/algoritmo usado.
/// </summary>
public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
