using Core.Domain.Entities;

namespace Core.Application.Abstractions;

public interface IDistributionService
{
    IReadOnlyCollection<Product> GetProducts();
}