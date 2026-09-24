using Core.Domain.Entities;

namespace Core.Application.Abstractions;

public interface IProductRepository
{
    IReadOnlyCollection<Product> GetAll();

    void Add(Product product);
}