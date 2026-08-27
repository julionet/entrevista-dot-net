namespace App.Application.DTOs;

public sealed record ProductDto(Guid Id, string Name, decimal Price);

public sealed record CreateProductRequest(string Name, decimal Price);

public sealed record ApplyDiscountRequest(decimal Percentage);
