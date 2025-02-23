using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Scannerfy.Api.Exceptions;

class ExceptionsHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var exceptionData = GetDataFromExcpetion(exception);

        httpContext.Response.StatusCode = exceptionData.StatusCode ?? 500;
        await httpContext.Response.WriteAsJsonAsync(exceptionData.Value, cancellationToken);

        return true;
    }

    public static ObjectResult GetDataFromExcpetion(Exception exception)
    {
        if (exception is UserFriendlyException userFriendlyException)
        {
            var result = new ObjectResult(new { userFriendlyException.Message, userFriendlyException.Code }) { StatusCode = 400 };
            return result;
        }

        return new ObjectResult(new { exception.Message }) { StatusCode = 500 };
    }
}