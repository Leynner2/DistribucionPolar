using Core.Application.DTOs;
using Core.Application.Services;
using Core.Application.Validators;
using Core.Domain.Entities;
using Core.Domain.Enums;
using FluentValidation;

namespace Core.Application.Tests;

public sealed class ServiceTests
{
    private readonly FakeCategoryRepository categories = new();
    private readonly FakeProductRepository products = new();
    private readonly FakeUserRepository users = new();
    private readonly FakeUnitOfWork unitOfWork = new();

    private ProductService CreateProductService() => new(
        products, categories, unitOfWork, new CreateProductRequestValidator(), new UpdateProductRequestValidator());

    private CategoryService CreateCategoryService() => new(
        categories, unitOfWork, new CreateCategoryRequestValidator(), new UpdateCategoryRequestValidator());

    private AuthService CreateAuthService() => new(users, new FakePasswordHasher(), new FakeTokenService(), new LoginRequestValidator());

    private Category AddFoodCategory()
    {
        var category = new Category("Alimentos", null, "Depósito #1 - Alimentos");
        categories.Add(category);
        return category;
    }

    private static CreateProductRequest HarinaPan(Guid categoryId) =>
        new("HARINA PAN 1KGx20UN", "P.A.N.", categoryId, 18.50m, 0.95m, 14.80m, 20, 2, 5, 100, 800, false, null);

    [Fact]
    public async Task CreateProduct_GeneratesSkuAndStoresStockInUnits()
    {
        var category = AddFoodCategory();

        var response = await CreateProductService().CreateAsync(HarinaPan(category.Id));

        Assert.Equal("ALI-HAR-0001", response.SKU);
        Assert.Equal(45, response.StockUnits);
        Assert.Equal("2 cajas + 5 und", response.StockDisplay);
        Assert.Equal("Depósito #1 - Alimentos", response.Deposit);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateProduct_SkipsSkuAlreadyTaken()
    {
        var category = AddFoodCategory();
        var service = CreateProductService();
        await service.CreateAsync(HarinaPan(category.Id));
        products.Products[0].MarkAsDeleted();

        var second = await service.CreateAsync(HarinaPan(category.Id));

        Assert.Equal("ALI-HAR-0002", second.SKU);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidData_ThrowsValidationAndSavesNothing()
    {
        var request = HarinaPan(AddFoodCategory().Id) with { PriceBox = -1 };

        await Assert.ThrowsAsync<ValidationException>(() => CreateProductService().CreateAsync(request));
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateProduct_WithUnknownCategory_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateProductService().CreateAsync(HarinaPan(Guid.NewGuid())));
    }

    [Fact]
    public async Task DeleteProduct_IsSoftDelete()
    {
        var created = await CreateProductService().CreateAsync(HarinaPan(AddFoodCategory().Id));

        await CreateProductService().DeleteAsync(created.Id);

        Assert.True(products.Products.Single().IsDeleted);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateProductService().GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteCategory_WithProducts_IsRejected()
    {
        var category = AddFoodCategory();
        categories.CategoriesWithProducts.Add(category.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateCategoryService().DeleteAsync(category.Id));
        Assert.False(category.IsDeleted);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicatedName_IsRejected()
    {
        AddFoodCategory();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateCategoryService().CreateAsync(new CreateCategoryRequest("ALIMENTOS", null, "Depósito #9")));
    }

    [Fact]
    public async Task Login_WithUsernameOrEmail_ReturnsTokenAndRole()
    {
        users.Add(new User("admin", "admin@distribucionpolar.local", "Administrador", "hash:Admin2026!", UserRole.Admin));
        var service = CreateAuthService();

        var byUsername = await service.LoginAsync(new LoginRequest(" ADMIN ", "Admin2026!"));
        var byEmail = await service.LoginAsync(new LoginRequest("Admin@DistribucionPolar.local", "Admin2026!"));

        Assert.Equal("token-admin", byUsername.Token);
        Assert.Equal("Admin", byUsername.Role);
        Assert.Equal("admin", byEmail.Username);
    }

    [Fact]
    public async Task Login_WithWrongPassword_IsUnauthorized()
    {
        users.Add(new User("admin", "admin@distribucionpolar.local", "Administrador", "hash:Admin2026!", UserRole.Admin));

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateAuthService().LoginAsync(new LoginRequest("admin", "otraclave")));
        Assert.Equal("Usuario o clave incorrectos.", error.Message);
    }

    [Fact]
    public async Task Login_WithInactiveUser_IsUnauthorized()
    {
        var user = new User("inactivo", "inactivo@distribucionpolar.local", "Inactivo", "hash:Inactivo2026!", UserRole.Employee);
        user.Deactivate();
        users.Add(user);

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateAuthService().LoginAsync(new LoginRequest("inactivo", "Inactivo2026!")));
        Assert.Equal("Este usuario está inactivo.", error.Message);
    }
}
