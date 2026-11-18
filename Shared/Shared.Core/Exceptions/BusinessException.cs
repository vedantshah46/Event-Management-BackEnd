namespace Shared.Core.Exceptions;

public enum BusinessError { Validation, NotFound, Forbidden, Conflict }

public sealed class BusinessException(BusinessError error, string message) : Exception(message)
{
    public BusinessError Error { get; } = error;
}
