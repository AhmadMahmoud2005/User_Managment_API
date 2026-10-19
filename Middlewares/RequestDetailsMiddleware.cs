using System;
using Microsoft.OpenApi;
using Serilog;

namespace UserManagementAPI.Middlewares;

public class RequestDetailsMiddleware
{
   private readonly RequestDelegate _next;
   public RequestDetailsMiddleware(RequestDelegate next)
   {
      _next = next;
   }

   public async Task InvokeAsync(HttpContext context)
   {
      var startTime = DateTime.UtcNow;
      Log.Information($"Path of Request: {context.Request.Path}");
      await _next(context);
      Log.Information($"Status code: {context.Response.StatusCode}");
      var duration = DateTime.UtcNow - startTime;
      Log.Information($"Duration: {duration}");
   }
}
