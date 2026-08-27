using App.Application.DTOs;
using App.Application.Ports.Input;
using App.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace App.WebApi.Controllers;

/// <summary>
/// Adaptador driving (primário): traduz requisições HTTP em chamadas à porta de entrada IProductService.
/// Não conhece Domain nem Infrastructure diretamente.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }

    [HttpPatch("{id:guid}/discount")]
    public async Task<ActionResult<ProductDto>> ApplyDiscount(Guid id, ApplyDiscountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.ApplyDiscountAsync(id, request, cancellationToken);
            return Ok(product);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
