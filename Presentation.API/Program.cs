using Core.Application.Abstractions;
using Core.Application.Services;
using Infrastructure.Persistence;
using Presentation.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddScoped<IDistributionService, DistributionService>();
builder.Services.AddTransient<IRequestIdGenerator, RequestIdGenerator>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Content(
    """
    <!DOCTYPE html>
    <html lang="es">
    <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Distribución Polar</title>
        <style>
            :root {
                color-scheme: light;
                font-family: Arial, sans-serif;
            }

            body {
                align-items: center;
                background: #f3f6f9;
                color: #17324d;
                display: flex;
                justify-content: center;
                margin: 0;
                min-height: 100vh;
            }

            main {
                background: white;
                border-radius: 16px;
                box-shadow: 0 12px 30px rgba(23, 50, 77, 0.12);
                max-width: 640px;
                padding: 48px;
                text-align: center;
                width: calc(100% - 64px);
            }

            h1 {
                color: #d71920;
                margin-top: 0;
            }

            p {
                line-height: 1.6;
            }
        </style>
    </head>
    <body>
        <main>
            <h1>Distribución Polar</h1>
            <p>La API está funcionando correctamente.</p>
            <p>Esta es la página inicial de prueba del sistema.</p>
        </main>
    </body>
    </html>
    """,
    "text/html; charset=utf-8"));

app.MapGet("/products", (IDistributionService distributionService) =>
    Results.Ok(distributionService.GetProducts()));

app.MapGet("/test/exception", (_) =>
    throw new Exception("Simulated unhandled exception for middleware testing."));

app.Run();
