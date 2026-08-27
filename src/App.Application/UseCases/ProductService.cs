using App.Application.DTOs;
using App.Application.Ports.Input;
using App.Application.Ports.Output;
using App.Domain.Entities;
using App.Domain.Exceptions;

namespace App.Application.UseCases;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = new Product(Guid.NewGuid(), request.Name, request.Price);

        await _productRepository.AddAsync(product, cancellationToken);

        return ToDto(product);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);

        return product is null ? null : ToDto(product);
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);

        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto> ApplyDiscountAsync(Guid id, ApplyDiscountRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException($"Produto '{id}' não encontrado.");

        product.ApplyDiscount(request.Percentage);

        await _productRepository.UpdateAsync(product, cancellationToken);

        return ToDto(product);
    }

    private static ProductDto ToDto(Product product) => new(product.Id, product.Name, product.Price);
}
