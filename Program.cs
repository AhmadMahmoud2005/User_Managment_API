using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
app.UseAuthentication();
app.UseAuthorization();

app.UseRequestCounter();

var users = new List<User>
{
    new User { Id = 1, Name = "Ahmad", Email = "ahmad@example.com", Age = 25 },
    new User { Id = 2, Name = "Sara", Email = "sara@example.com", Age = 28 },
    new User { Id = 3, Name = "Omar", Email = "omar@example.com", Age = 30 },
    new User { Id = 4, Name = "Laila", Email = "laila@example.com", Age = 22 },
    new User { Id = 5, Name = "Hassan", Email = "hassan@example.com", Age = 35 }
};

app.MapGet("/", () => "Hello from root").ExcludeFromDescription();
app.MapGet("/users/count", (RequestCountService counter) => counter.GetAllCounts());

// GET all users
app.MapGet("/users", (int? skip, int? take) =>
{
    var res = users.Skip(skip ?? 0).Take(take ?? users.Count).ToList();
    return Results.Ok(res);
})
.WithName("GetUsers");

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
