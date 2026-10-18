using System;
using System.Diagnostics.Metrics;

namespace UserManagementAPI.Middlewares;

public class RequestCounterMiddleware
{
   private readonly RequestCountService _counterService;
   private readonly RequestDelegate _next;
   public RequestCounterMiddleware(RequestCountService counterService, RequestDelegate next)
   {
      _counterService = counterService;
      _next = next;
   }
   public async Task InvokeAsync(HttpContext context)
   {
      var path = context.Request.Path.ToString();
      _counterService.IncrementCount(path);

      context.Response.OnStarting(() =>
      {
         context.Response.Headers["X-Request-Count"] = _counterService.GetCount(path).ToString();
         return Task.CompletedTask;
      });

      await _next(context);
   }
}
