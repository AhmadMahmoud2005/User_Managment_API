USER MANAGEMENT — One-file crash pack (fast copy-paste)

Goal
- Get UserManagementAPI to a working final-project baseline today: user model, service, DI, middleware auth, logging, JSON export/import, and a tiny client test.

Quick plan (3–4 hours)
1) Add User model (5m)
2) Add IUserService + UserService and register DI (20m)
3) Add UsersController with register/login/export/import (45m)
4) Add simple AuthMiddleware (20m)
5) Configure Serilog (20m)
6) Run and test with ClientApi test script (30m)

Files to create/update (paths relative to repo root)
- UserManagementAPI/Models/User.cs
- UserManagementAPI/Services/IUserService.cs
- UserManagementAPI/Services/UserService.cs
- UserManagementAPI/Middleware/AuthMiddleware.cs
- UserManagementAPI/Controllers/UsersController.cs
- UserManagementAPI/Program.cs (small edits)
- ClientApi/SimpleUserTest.cs (optional)

1) User model (UserManagementAPI/Models/User.cs)
--- copy-paste ---
using System;
using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Username { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; } // store hashed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
--- end ---

Note: For now store users in memory; later swap to DB.

2) IUserService + UserService (in-memory) (UserManagementAPI/Services)
IUserService.cs
---
using System;
using System.Collections.Generic;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public interface IUserService
    {
        User Create(User user, string plainPassword);
        User GetByUsername(string username);
        IEnumerable<User> GetAll();
        bool ValidateCredentials(string username, string password);
    }
}
---

UserService.cs
---
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net; // if not available use a simple hash for now
using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public class UserService : IUserService
    {
        private readonly ConcurrentDictionary<string, User> _users = new();

        public User Create(User user, string plainPassword)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            _users[user.Username.ToLower()] = user;
            return user;
        }

        public User GetByUsername(string username) =>
            _users.TryGetValue(username.ToLower(), out var u) ? u : null;

        public IEnumerable<User> GetAll() => _users.Values.ToList();

        public bool ValidateCredentials(string username, string password)
        {
            var u = GetByUsername(username);
            return u != null && BCrypt.Net.BCrypt.Verify(password, u.PasswordHash);
        }
    }
}
---

3) UsersController (UserManagementAPI/Controllers/UsersController.cs)
---
using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;
using System.Collections.Generic;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _users;
        public UsersController(IUserService users) => _users = users;

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            if (_users.GetByUsername(dto.Username) != null)
                return Conflict("username exists");
            var user = new User { Username = dto.Username, Email = dto.Email };
            _users.Create(user, dto.Password);
            return CreatedAtAction(nameof(Get), new { username = user.Username }, user);
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            if (_users.ValidateCredentials(dto.Username, dto.Password))
                return Ok(new { token = "fake-jwt-token-for-now" });
            return Unauthorized();
        }

        [HttpGet]
        public IEnumerable<User> GetAll() => _users.GetAll();

        [HttpGet("{username}")]
        public IActionResult Get(string username)
        {
            var u = _users.GetByUsername(username);
            return u == null ? NotFound() : Ok(u);
        }

        [HttpGet("export/json")]
        public IActionResult ExportJson() => Ok(_users.GetAll());
    }

    public record RegisterDto(string Username, string Email, string Password);
    public record LoginDto(string Username, string Password);
}
---

4) Auth middleware (UserManagementAPI/Middleware/AuthMiddleware.cs)
---
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace UserManagementAPI.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;
        public AuthMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx)
        {
            // Simple header-based "auth" for dev: set X-Api-Key: secret
            if (ctx.Request.Path.StartsWithSegments("/api/users/protected") )
            {
                if (!ctx.Request.Headers.TryGetValue("X-Api-Key", out var v) || v != "secret")
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await ctx.Response.WriteAsync("Unauthorized");
                    return;
                }
            }
            await _next(ctx);
        }
    }
}
---

In Program.cs register middleware and DI:
--- edit Program.cs ---
// after builder.Services.*
builder.Services.AddSingleton<IUserService, UserService>();

var app = builder.Build();
app.UseMiddleware<UserManagementAPI.Middleware.AuthMiddleware>();
app.MapControllers();
app.Run();
--- end ---

5) Serilog quick setup (Program.cs + nuget)
- Add package Serilog.AspNetCore and Serilog.Sinks.File
- In Program.cs before building, add:
using Serilog;
Log.Logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day).CreateLogger();
builder.Host.UseSerilog();

6) Quick client test (ClientApi/SimpleUserTest.cs)
---
using System.Net.Http;
using System.Text.Json;
using System.Text;

var client = new HttpClient { BaseAddress = new Uri("https://localhost:5001/") };
var register = new { Username = "alice", Email = "a@a.com", Password = "Pass123!" };
var res = await client.PostAsync("api/users/register", new StringContent(JsonSerializer.Serialize(register), Encoding.UTF8, "application/json"));
Console.WriteLine(res.StatusCode);
---

Testing notes
- Run UserManagementAPI: dotnet run --project UserManagementAPI
- Use ClientApi or curl to test endpoints
- For protected route test, include header X-Api-Key: secret

Keep it tiny
- This uses in-memory storage and a fake token to let you get the features wired quickly. After this baseline, we can replace storage with EF Core and integrate proper JWT auth.

If you want, confirm and I will create the files (the exact code above) in the repository for you now.