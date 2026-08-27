using App.Domain.Exceptions;

namespace App.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    public Product(Guid id, string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do produto é obrigatório.");

        if (price <= 0)
            throw new DomainException("O preço do produto deve ser maior que zero.");

        Id = id;
        Name = name;
        Price = price;
    }

    public void ApplyDiscount(decimal percentage)
    {
        if (percentage is <= 0 or >= 100)
            throw new DomainException("O percentual de desconto deve estar entre 0 e 100.");

        Price -= Price * percentage / 100;
    }
}
