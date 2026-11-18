using Microsoft.AspNetCore.Diagnostics;
using Shared.Core.Exceptions;

namespace EventProject.Configuration;

public sealed class BusinessExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessException business) return false;
        var status = business.Error switch
        {
            BusinessError.Validation => StatusCodes.Status400BadRequest,
            BusinessError.NotFound => StatusCodes.Status404NotFound,
            BusinessError.Forbidden => StatusCodes.Status403Forbidden,
            BusinessError.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        await Results.Problem(statusCode: status, title: business.Message).ExecuteAsync(context);
        return true;
    }
}
