using App.Application.DTOs;

namespace App.Application.Ports.Input;

/// <summary>
/// Porta de entrada (driving port): contrato que os adaptadores externos (ex.: WebApi, CLI, testes)
/// usam para acionar os casos de uso da aplicação. Implementada por ProductService.
/// </summary>
public interface IProductService
{
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProductDto> ApplyDiscountAsync(Guid id, ApplyDiscountRequest request, CancellationToken cancellationToken = default);
}
