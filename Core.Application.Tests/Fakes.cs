using Core.Application.Abstractions;
using Core.Domain.Entities;

namespace Core.Application.Tests;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        operation(cancellationToken);
}

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    public List<Category> Categories { get; } = [];

    public HashSet<Guid> CategoriesWithProducts { get; } = [];

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Category>>(Categories.Where(c => !c.IsDeleted).ToList());

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Categories.FirstOrDefault(c => c.Id == id && !c.IsDeleted));

    public Task<Category?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Categories.Any(c =>
            !c.IsDeleted && c.Id != excludeId && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(CategoriesWithProducts.Contains(categoryId));

    public void Add(Category category) => Categories.Add(category);
}

internal sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Products { get; } = [];

    public Task<IReadOnlyList<Product>> GetAllAsync(Guid? categoryId, string? search, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(Products.Where(p => !p.IsDeleted).ToList());

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Products.FirstOrDefault(p => p.Id == id && !p.IsDeleted));

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(Products.FirstOrDefault(p => p.SKU == sku && !p.IsDeleted));

    public Task<IReadOnlyList<Product>> GetLowStockAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(Products.Where(p => !p.IsDeleted && p.StockUnits <= p.MinStock).ToList());

    public Task<IReadOnlyList<Product>> GetGiftEligibleAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(Products.Where(p => !p.IsDeleted && p.IsGiftEligible).ToList());

    public Task<IReadOnlyDictionary<Guid, Product>> GetManyForUpdateAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, Product>>(Products.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id));

    public Task<Product?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        Task.FromResult(Products.Any(p => p.SKU == sku));

    public Task<int> CountIncludingDeletedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Products.Count);

    public void Add(Product product) => Products.Add(product);
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByUsernameOrEmailAsync(string login, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Username == login || u.Email == login));

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(Users.ToList());

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => GetByIdAsync(id, cancellationToken);

    public Task<bool> UsernameOrEmailExistsAsync(string username, string email, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.Any(u => u.Id != excludeId && (u.Username == username || u.Email == email)));

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Task.FromResult(Users.Count > 0);

    public void Add(User user) => Users.Add(user);
}

/// <summary>Hasher trivial para pruebas: "hash:" + clave.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;

    public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
}

internal sealed class FakeTokenService : ITokenService
{
    public AccessToken GenerateToken(User user) => new($"token-{user.Username}", new DateTime(2026, 10, 1, 1, 0, 0, DateTimeKind.Utc));
}
