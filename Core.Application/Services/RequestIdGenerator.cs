using Core.Application.Abstractions;

namespace Core.Application.Services;

public sealed class RequestIdGenerator : IRequestIdGenerator
{
    public Guid Generate()
    {
        return Guid.NewGuid();
    }
}