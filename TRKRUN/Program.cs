using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TRKRUN.Context;
using TRKRUN.Identity;
using TRKRUN.Services;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURACIÓN PARA RAILWAY - Puerto dinámico
var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 1. Añadir política de CORS - CORRECCIÓN PARA RAILWAY
var corsOrigins = new List<string>
{
    "http://localhost:4200",
    "http://192.168.1.134:4200",
    "http://192.168.56.1:4200",
    "https://trkrun.netlify.app",
    "https://norman-main-velvet-ticket.trycloudflare.com"
};

// CORRECCIÓN: Railway proporciona diferentes variables de entorno
var railwayUrl = Environment.GetEnvironmentVariable("RAILWAY_STATIC_URL")
                ?? Environment.GetEnvironmentVariable("RAILWAY_PUBLIC_DOMAIN");

if (!string.IsNullOrEmpty(railwayUrl))
{
  // Si es RAILWAY_STATIC_URL ya viene con https://
  if (railwayUrl.StartsWith("https://"))
  {
    corsOrigins.Add(railwayUrl);
  }
  else
  {
    corsOrigins.Add($"https://{railwayUrl}");
  }
}

// También agregar la URL específica de tu deployment actual
corsOrigins.Add("https://apitrkrun-production.up.railway.app");

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAngularClient", policy =>
  {
    policy
        .WithOrigins(corsOrigins.ToArray())
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // IMPORTANTE: Habilitar credenciales si usas autenticación
  });
});

// 2. Añadir controladores y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Añadir Authorization (necesario para JWT)
builder.Services.AddAuthorization();

// 4. CONFIGURAR BASE DE DATOS - Railway vs Local
string connectionString;
var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");

if (!string.IsNullOrEmpty(mysqlUrl))
{
  // Estamos en Railway - convertir URL
  Console.WriteLine("Usando base de datos de Railway");
  var uri = new Uri(mysqlUrl);
  connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Uid={uri.UserInfo.Split(':')[0]};Pwd={uri.UserInfo.Split(':')[1]};";
}
else
{
  // Estamos en desarrollo local
  Console.WriteLine("Usando base de datos local");
  connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// 5. Registrar PasswordHasher<User> (para hashear/verificar contraseñas)
builder.Services.AddScoped<PasswordHasher<User>>();

// 6. Registrar AuthService e interfaz IAuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// 7. Registrar tus demás servicios personalizados
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITorneoService, TorneoService>();
builder.Services.AddScoped<ICircuitoService, CircuitoService>();
builder.Services.AddScoped<IRolService, RolService>();

// 8. CONFIGURAR JWT
string jwtKey, jwtIssuer, jwtAudience;

jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];
jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"];
jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey))
{
  throw new InvalidOperationException("JWT Key no está configurada. Verifica JWT_KEY en variables de entorno o Jwt:Key en appsettings.json");
}

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
  options.RequireHttpsMetadata = builder.Environment.IsProduction();
  options.SaveToken = true;
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = jwtIssuer,
    ValidAudience = jwtAudience,
    IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
  };
});

var app = builder.Build();

// 9. Aplicar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  try
  {
    Console.WriteLine("Aplicando migraciones de base de datos...");
    db.Database.Migrate();
    Console.WriteLine("Migraciones aplicadas exitosamente");
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Error aplicando migraciones: {ex.Message}");
    if (app.Environment.IsProduction())
    {
      throw;
    }
  }
}

// 10. Configuración de middleware
if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}

// 11. Habilitar Swagger en todos los entornos para debugging
app.UseSwagger();
app.UseSwaggerUI();

// No usar HTTPS redirect en Railway
if (!app.Environment.IsProduction())
{
  app.UseHttpsRedirection();
}

// 12. CORS DEBE IR ANTES de Authentication/Authorization
app.UseCors("AllowAngularClient");

// 13. Autenticación → Autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint de salud mejorado
app.MapGet("/health", () => new
{
  Status = "Healthy",
  Environment = app.Environment.EnvironmentName,
  Database = !string.IsNullOrEmpty(mysqlUrl) ? "Railway MySQL" : "Local MySQL",
  HasJwtKey = !string.IsNullOrEmpty(jwtKey),
  Port = port,
  CorsOrigins = corsOrigins, // Para debugging
  RailwayUrl = railwayUrl,   // Para debugging
  Timestamp = DateTime.UtcNow
});

// Endpoint específico para verificar CORS
app.MapGet("/cors-test", () => new { Message = "CORS está funcionando!" });

Console.WriteLine($"Aplicación iniciando en puerto: {port}");
Console.WriteLine($"Entorno: {app.Environment.EnvironmentName}");
Console.WriteLine($"Base de datos: {(!string.IsNullOrEmpty(mysqlUrl) ? "Railway" : "Local")}");
Console.WriteLine($"CORS Origins configurados: {string.Join(", ", corsOrigins)}");

app.Run();
