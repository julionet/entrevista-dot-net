using App.Domain.Entities;

namespace App.Application.Ports.Output;

/// <summary>
/// Porta de saída (driven port): contrato que a Application exige da infraestrutura
/// para persistir e consultar usuários.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
