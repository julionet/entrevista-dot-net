using App.Application.DTOs;
using App.Application.Ports.Output;
using App.Application.UseCases;
using App.Domain.Entities;
using App.Domain.Exceptions;
using Moq;
using Xunit;

namespace App.Application.Tests;

public sealed class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_DevePersistirEDevolverProdutoCriado()
    {
        var request = new CreateProductRequest("Teclado Mecânico", 350m);

        var result = await _sut.CreateAsync(request);

        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Price, result.Price);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ComPrecoInvalido_DeveLancarDomainException()
    {
        var request = new CreateProductRequest("Mouse", 0m);

        await Assert.ThrowsAsync<DomainException>(() => _sut.CreateAsync(request));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyDiscountAsync_ComProdutoExistente_DeveAplicarDescontoEAtualizar()
    {
        var product = new Product(Guid.NewGuid(), "Monitor", 1000m);
        _repositoryMock.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _sut.ApplyDiscountAsync(product.Id, new ApplyDiscountRequest(10m));

        Assert.Equal(900m, result.Price);
        _repositoryMock.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyDiscountAsync_ComProdutoInexistente_DeveLancarDomainException()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.ApplyDiscountAsync(id, new ApplyDiscountRequest(10m)));
    }
}
