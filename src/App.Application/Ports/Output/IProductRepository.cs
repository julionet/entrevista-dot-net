using App.Domain.Entities;

namespace App.Application.Ports.Output;

/// <summary>
/// Porta de saída (driven port): contrato que a Application exige da infraestrutura
/// para persistir e consultar produtos. Implementada por um adaptador em App.Infrastructure.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}
