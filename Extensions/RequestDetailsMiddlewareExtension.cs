using System;
using UserManagementAPI.Middlewares;

namespace UserManagementAPI.Extensions;

public static class RequestDetailsMiddlewareExtension
{
   public static IApplicationBuilder UseRequestDetails(this IApplicationBuilder builder)
   {
      return builder.UseMiddleware<RequestDetailsMiddleware>();
   }
}
