using Core.Application.Abstractions;
using Core.Domain.Entities;

namespace Core.Application.Services;

public sealed class DistributionService(IProductRepository productRepository) : IDistributionService
{
    public IReadOnlyCollection<Product> GetProducts()
    {
        return productRepository.GetAll();
    }
}