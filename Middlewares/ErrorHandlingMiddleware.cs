using System;
using System.Net;
using System.Text.Json;

namespace UserManagementAPI.Middlewares;

public class ErrorHandlingMiddleware
{
   private readonly RequestDelegate _next;
   private readonly ILogger<ErrorHandlingMiddleware> _logger;
   public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
   {
      _next = next;
      _logger = logger;
   }

   public async Task InvokeAsync(HttpContext context)
   {
      try
      {
         await _next(context);
      }
      catch (Exception ex)
      {
         _logger.LogError(
            ex,
            "Unhandled exception for {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

         if (context.Response.HasStarted)
         {
            throw;
         }

         context.Response.Clear();
         context.Response.StatusCode = StatusCodes.Status500InternalServerError;
         context.Response.ContentType = "application/json";

         await context.Response.WriteAsJsonAsync(new
         {
            error = "An unexpected error occurred."
         });
      }
   }
}
