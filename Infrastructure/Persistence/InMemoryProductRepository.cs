using Core.Application.Abstractions;
using Core.Domain.Entities;

namespace Infrastructure.Persistence;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly List<Product> products =
    [
        new Product("Harina de maiz precocida", 2.50m),
        new Product("Bebida maltin", 1.75m)
    ];

    public IReadOnlyCollection<Product> GetAll()
    {
        lock (products)
        {
            return products.ToArray();
        }
    }

    public void Add(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        lock (products)
        {
            products.Add(product);
        }
    }
}