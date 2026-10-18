using System;
using UserManagementAPI.Middlewares;

namespace UserManagementAPI.Extensions;

public static class RequestCounterMiddlewareExtension
{
   public static IApplicationBuilder UseRequestCounter(this IApplicationBuilder builder)
   {
      return builder.UseMiddleware<RequestCounterMiddleware>();
   }
}
