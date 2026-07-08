using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Retsuko.Plugins;

public class GlobalExceptionHandler: IExceptionHandler {
  public async ValueTask<bool> TryHandleAsync(
    HttpContext context,
    Exception exception,
    CancellationToken cancellationToken
  ) {
    context.Response.StatusCode = 500;
    context.Response.ContentType = "application/json";

    EventDispatcher.Exception(context, exception);

    var result = JsonSerializer.Serialize(new { error = exception.Message });
    await context.Response.WriteAsync(result, cancellationToken);

    return true;
  }
}
