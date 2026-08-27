using Microsoft.EntityFrameworkCore;
using App.Application.Ports.Output;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador driven (secundário): implementação concreta da porta IProductRepository usando EF Core.
/// Poderia ser substituída por outro adaptador (ex.: outro ORM ou um client HTTP) sem alterar Domain ou Application.
/// </summary>
public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _dbContext;

    public ProductRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.Products.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(product).State == EntityState.Detached)
            _dbContext.Products.Update(product);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
