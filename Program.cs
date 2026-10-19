using System.Diagnostics.Metrics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using UserManagementAPI.Extensions;
using UserManagementAPI.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RequestCountService>();
// builder.Services.AddScoped<RequestCountService>();
// builder.Services.AddTransient<RequestCountService>();
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 2;
        options.Window = TimeSpan.FromSeconds(5);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });

    options.OnRejected = (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("RateLimiting");

        logger.LogWarning(
            "Rate limit rejected request {Method} {Path}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path
        );

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return ValueTask.CompletedTask;
    };
});
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
else
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.UseRequestCounter();
app.UseRequestDetails();

var users = new List<User>
{
    new User { Id = 1, Name = "Ahmad", Email = "ahmad@example.com", Age = 25 },
    new User { Id = 2, Name = "Sara", Email = "sara@example.com", Age = 28 },
    new User { Id = 3, Name = "Omar", Email = "omar@example.com", Age = 30 },
    new User { Id = 4, Name = "Laila", Email = "laila@example.com", Age = 22 },
    new User { Id = 5, Name = "Hassan", Email = "hassan@example.com", Age = 35 }
};

app.MapGet("/", () => "Hello from root").ExcludeFromDescription();
app.MapGet("/users/counts", (RequestCountService counter) => counter.GetAllCounts())
.RequireRateLimiting("fixed");

// GET all users
app.MapGet("/users", (int? skip, int? take) =>
{
    var res = users.Skip(skip ?? 0).Take(take ?? users.Count).ToList();
    return Results.Ok(res);
})
.WithName("GetUsers")
.RequireRateLimiting("fixed");

// GET user by id
app.MapGet("/users/{id:int}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound(new { mesage = $"There's no user with id: {id}"});
})
.WithName("GetUserById");

// POST new user
app.MapPost("/users", (User newUser) =>
{
    if (!UserValidator.IsValid(newUser))
        return Results.BadRequest(new {message = "Invalid user data"});
    // Assign a new Id automatically
    newUser.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
    users.Add(newUser);
    return Results.Created($"/users/{newUser.Id}", newUser);
})
.WithName("CreateUser");

// PUT new user
var updateRoute = app.MapPut("/users/{id:int:min(0)}", (int id, User newUser) => {
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user == null) return Results.NotFound($"{id} not found");
    if (!UserValidator.IsValid(newUser)) return Results.BadRequest(new {message = "Invalid user data"});

    user.Name = newUser.Name;
    user.Email= newUser.Email;
    user.Age = newUser.Age ;

    return Results.Ok(user);
})
.WithName("UpdateUser");

// Delete user by id
var deleteRoute = app.MapDelete("/users/{id:int:min(0)}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user == null) return Results.NotFound($"{id} not found");
    users.Remove(user);
    return Results.NoContent();
})
.WithName("DeleteUser");

if (app.Environment.IsDevelopment())
{
    updateRoute.AllowAnonymous();
    deleteRoute.AllowAnonymous();
}
else
{
    updateRoute.RequireAuthorization();
    deleteRoute.RequireAuthorization();
}

app.Run();
