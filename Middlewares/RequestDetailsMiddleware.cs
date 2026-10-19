using System;
using System.Diagnostics;
using Microsoft.OpenApi;

namespace UserManagementAPI.Middlewares;

public class RequestDetailsMiddleware
{
   private readonly RequestDelegate _next;
   private readonly ILogger<RequestDetailsMiddleware> _logger;
   public RequestDetailsMiddleware(RequestDelegate next, ILogger<RequestDetailsMiddleware> logger)
   {
      _next = next;
      _logger = logger;
   }

   public async Task InvokeAsync(HttpContext context)
   {
      var startTime = Stopwatch.GetTimestamp();
      _logger.LogInformation($"Path of Request: {context.Request.Path}");
      try
      {
         await _next(context);
      }
      finally
      {
         _logger.LogInformation($"Status code: {context.Response.StatusCode}");
         var duration = Stopwatch.GetElapsedTime(startTime);
         _logger.LogInformation($"Duration: {duration}");
      }
   }
}
