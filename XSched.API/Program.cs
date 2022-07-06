using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using XSched.API.DbContexts;
using XSched.API.Entities;
using XSched.API.Helpers;
using XSched.API.Orchestrators.Implementations;
using XSched.API.Orchestrators.Interfaces;
using XSched.API.Services.Implementations;
using XSched.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureAppConfiguration((_, config) => { config.AddJsonFile("appsettings.local.json", false, true); });
var configuration = builder.Configuration;

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<XSchedDbContext>();
// AddAuthentication should go after AddIdentity as AddIdentity sets default auth options as well
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = null;
}).AddJwtBearer(options =>
{
    // options.SaveToken = true;
    // options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["JWT:ValidIssuer"],
        ValidAudience = configuration["JWT:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetJwtString("Secret")))
    };
});

builder.Services.AddTransient<IAuthenticateOrchestrator, AuthenticateOrchestrator>();
builder.Services.AddTransient<IJwtTokenService, JwtTokenService>();
builder.Services.AddDbContext<XSchedDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("XSchedDb"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();