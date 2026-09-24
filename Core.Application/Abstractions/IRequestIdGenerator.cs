namespace Core.Application.Abstractions;

public interface IRequestIdGenerator
{
    Guid Generate();
}