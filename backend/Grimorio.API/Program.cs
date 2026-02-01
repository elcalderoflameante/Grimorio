using Grimorio.Infrastructure;
using Grimorio.Infrastructure.Persistence;
using Grimorio.Infrastructure.Security;
using Grimorio.Infrastructure.Seeding;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using DotNetEnv;
using System.Text;

// Carga variables de entorno desde .env (solo en desarrollo)
if (File.Exists("../../.env"))
{
    Env.Load("../../.env");
}

var builder = WebApplication.CreateBuilder(args);

// === Configurar Serilog ===
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/grimorio_.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// === Construir cadena de conexión desde variables de entorno ===
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "grimorio_dev";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";
builder.Configuration.GetSection("ConnectionStrings")["DefaultConnection"] = connectionString;

// === Configurar JWT ===
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] ?? "your-default-secret-key-change-in-production";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "Grimorio";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "GrimorioClient";

var key = Encoding.ASCII.GetBytes(jwtSecretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// === CORS ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// === Authorization handler para bypass Admin ===
builder.Services.AddSingleton<IAuthorizationHandler, Grimorio.API.Authorization.AdminBypassHandler>();

// === Authorization policies ===
builder.Services.AddAuthorization(options =>
{
    // Política básica para roles
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Administrador"));

    // Políticas granulares por permiso (RRHH)
    // El AdminBypassHandler bypasea estas políticas automáticamente si el usuario es Administrador
    options.AddPolicy("RRHH.ViewEmployees", policy =>
        policy.RequireAssertion(context =>
        {
            var permissionsClaims = context.User.FindAll("permissions");
            return permissionsClaims.Any(c => c.Value == "RRHH.ViewEmployees");
        }));

    options.AddPolicy("RRHH.CreateEmployees", policy =>
        policy.RequireAssertion(context =>
        {
            var permissionsClaims = context.User.FindAll("permissions");
            return permissionsClaims.Any(c => c.Value == "RRHH.CreateEmployees");
        }));

    options.AddPolicy("RRHH.UpdateEmployees", policy =>
        policy.RequireAssertion(context =>
        {
            var permissionsClaims = context.User.FindAll("permissions");
            return permissionsClaims.Any(c => c.Value == "RRHH.UpdateEmployees");
        }));

    options.AddPolicy("RRHH.DeleteEmployees", policy =>
        policy.RequireAssertion(context =>
        {
            var permissionsClaims = context.User.FindAll("permissions");
            return permissionsClaims.Any(c => c.Value == "RRHH.DeleteEmployees");
        }));
});

// === Add services to the container ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// === AutoMapper ===
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<Grimorio.Infrastructure.Mappings.MappingProfile>());

// === Registrar servicios de infraestructura ===
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHashingService, PasswordHashingService>();

// === Registrar MediatR ===
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Grimorio.Application.DTOs.LoginRequest).Assembly);
    config.RegisterServicesFromAssembly(typeof(Grimorio.Infrastructure.Persistence.GrimorioDbContext).Assembly);
});

var app = builder.Build();

// === Ejecutar migraciones de EF Core al iniciar (en desarrollo) ===
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
        await dbContext.Database.MigrateAsync();

        // Ejecutar seeder
        var passwordHashingService = scope.ServiceProvider.GetRequiredService<IPasswordHashingService>();
        await AuthSeeder.SeedAsync(dbContext, passwordHashingService);
    }
}

// === Configure the HTTP request pipeline ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Seed scheduling data
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<GrimorioDbContext>();
        await SchedulingSeeder.SeedSchedulingDataAsync(dbContext);
    }
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
