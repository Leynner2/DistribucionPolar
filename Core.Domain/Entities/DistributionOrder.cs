namespace Core.Domain.Entities;

public sealed class DistributionOrder : BaseEntity
{
    private readonly List<Guid> productIds = [];

    public DistributionOrder(string customerName, IEnumerable<Guid> productIds)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException("Customer name is required.", nameof(customerName));
        }

        ArgumentNullException.ThrowIfNull(productIds);

        this.productIds.AddRange(productIds);
        if (this.productIds.Count == 0)
        {
            throw new ArgumentException("At least one product is required.", nameof(productIds));
        }

        CustomerName = customerName;
    }

    public string CustomerName { get; }

    public IReadOnlyCollection<Guid> ProductIds => productIds.AsReadOnly();
}