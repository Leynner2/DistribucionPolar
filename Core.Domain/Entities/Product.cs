namespace Core.Domain.Entities;

public sealed class Product : BaseEntity
{
    public Product(string name, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice));
        }

        Name = name;
        UnitPrice = unitPrice;
    }

    public string Name { get; }

    public decimal UnitPrice { get; }
}