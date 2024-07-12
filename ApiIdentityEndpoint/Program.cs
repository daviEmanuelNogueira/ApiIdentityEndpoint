using ApiIdentityEndpoint.Data;
using ApiIdentityEndpoint.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options 
    => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<User>()
    .AddEntityFrameworkStores<AppDbContext>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", (ClaimsPrincipal user) => user.Identity!.Name).RequireAuthorization();

app.MapIdentityApi<User>();

app.MapPost("/logout", async (SignInManager<User> signInManager, [FromBody] object empty) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});

app.MapPost("/unlock-user", async (UserManager<User> userManager, string email) =>
{
    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    var resetAccessFailedCountResult = await userManager.ResetAccessFailedCountAsync(user);
    if (!resetAccessFailedCountResult.Succeeded)
    {
        return Results.BadRequest("Failed to reset access failed count");
    }

    var setLockoutEndDateResult = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
    if (!setLockoutEndDateResult.Succeeded)
    {
        return Results.BadRequest("Failed to set lockout end date");
    }

    return Results.Ok("User unlocked successfully");
}).RequireAuthorization();

app.Run();