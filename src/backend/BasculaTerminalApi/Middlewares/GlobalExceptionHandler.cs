using Microsoft.AspNetCore.Diagnostics;

namespace BasculaTerminalApi.Middlewares
{
    public static class GlobalExceptionHandler
    {
        public static void UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(builder =>
            {
                builder.Run(async context =>
                {
                    context.Response.ContentType = "application/json";

                    Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                    if (exception != null)
                    {
                        context.Response.StatusCode = exception switch
                        {
                            InvalidOperationException => StatusCodes.Status400BadRequest,
                            NotImplementedException => StatusCodes.Status501NotImplemented,
                            KeyNotFoundException => StatusCodes.Status400BadRequest,
                            _ => StatusCodes.Status500InternalServerError
                        };

                        // Log the exception (optional)
                        // Log.Error(exception, "An unhandled exception occurred.");
                        await context.Response.WriteAsJsonAsync(new
                        {
                            context.Response.StatusCode,
                            Message = "An unexpected error occurred.",
                            Details = exception.Message
                        });
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            context.Response.StatusCode,
                            Message = "An unexpected error occurred.",
                            Details = "No exception details available."
                        });
                    }
                });
            });
        }
    }
}
