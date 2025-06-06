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
    // TEMPORALMENTE más permisivo para debugging
    if (builder.Environment.IsDevelopment())
    {
      policy
          .WithOrigins(corsOrigins.ToArray())
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
    }
    else
    {
      // En producción, usar configuración más permisiva temporalmente
      policy
          .AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
      // Nota: AllowAnyOrigin() no permite AllowCredentials()
    }
  });
});

// 2. Añadir controladores y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Añadir Authorization (necesario para JWT)
builder.Services.AddAuthorization();

// 4. CONFIGURAR BASE DE DATOS - Railway vs Local MEJORADO
string connectionString;
var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");

if (!string.IsNullOrEmpty(mysqlUrl))
{
  // Estamos en Railway - usar las variables individuales es más confiable
  Console.WriteLine("Usando base de datos de Railway");
  Console.WriteLine($"MYSQL_URL detectada: {mysqlUrl.Substring(0, Math.Min(30, mysqlUrl.Length))}...");

  try
  {
    // Método 1: Usar variables individuales (más confiable)
    var host = Environment.GetEnvironmentVariable("MYSQLHOST");
    var port = Environment.GetEnvironmentVariable("MYSQLPORT");
    var database = Environment.GetEnvironmentVariable("MYSQLDATABASE");
    var user = Environment.GetEnvironmentVariable("MYSQLUSER");
    var password = Environment.GetEnvironmentVariable("MYSQLPASSWORD");

    if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(database))
    {
      connectionString = $"Server={host};Port={port ?? "3306"};Database={database};Uid={user};Pwd={password};";
      Console.WriteLine($"Usando variables individuales: Server={host}, Database={database}");
    }
    else
    {
      // Método 2: Parsear MYSQL_URL como fallback
      var uri = new Uri(mysqlUrl);
      var userInfo = uri.UserInfo.Split(':');
      connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Uid={userInfo[0]};Pwd={userInfo[1]};";
      Console.WriteLine($"Usando MYSQL_URL parseada: Server={uri.Host}, Database={uri.AbsolutePath.TrimStart('/')}");
    }
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Error parseando MYSQL_URL: {ex.Message}");
    // Fallback usando MYSQL_PUBLIC_URL si está disponible
    var publicUrl = Environment.GetEnvironmentVariable("MYSQL_PUBLIC_URL");
    if (!string.IsNullOrEmpty(publicUrl))
    {
      var uri = new Uri(publicUrl);
      var userInfo = uri.UserInfo.Split(':');
      connectionString = $"Server={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Uid={userInfo[0]};Pwd={userInfo[1]};";
    }
    else
    {
      throw new InvalidOperationException($"No se pudo configurar la conexión a MySQL: {ex.Message}");
    }
  }
}
else
{
  // Estamos en desarrollo local
  Console.WriteLine("Usando base de datos local");
  connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

Console.WriteLine($"Connection string configurado (primeros 50 chars): {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");

builder.Services.AddDbContext<AppDbContext>(opts =>
{
  opts.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
      mySqlOptions =>
      {
        // Configuraciones adicionales para Railway/MySQL
        mySqlOptions.EnableRetryOnFailure(
              maxRetryCount: 5,
              maxRetryDelay: TimeSpan.FromSeconds(30),
              errorNumbersToAdd: null);
      });

  // Habilitar logging de SQL en desarrollo
  if (builder.Environment.IsDevelopment())
  {
    opts.EnableSensitiveDataLogging();
    opts.LogTo(Console.WriteLine);
  }
});

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

// 9. Aplicar migraciones automáticamente CON MEJOR MANEJO DE ERRORES
using (var scope = app.Services.CreateScope())
{
  var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
  try
  {
    Console.WriteLine("Verificando conexión a base de datos...");

    // Probar conexión primero
    var canConnect = db.Database.CanConnect();
    Console.WriteLine($"¿Puede conectar a la BD? {canConnect}");

    if (canConnect)
    {
      Console.WriteLine("Verificando migraciones pendientes...");
      var pendingMigrations = db.Database.GetPendingMigrations().ToList();
      Console.WriteLine($"Migraciones pendientes: {pendingMigrations.Count}");

      if (pendingMigrations.Any())
      {
        Console.WriteLine("Aplicando migraciones de base de datos...");
        foreach (var migration in pendingMigrations)
        {
          Console.WriteLine($"- {migration}");
        }

        db.Database.Migrate();
        Console.WriteLine("Migraciones aplicadas exitosamente");
      }
      else
      {
        Console.WriteLine("No hay migraciones pendientes");
      }
    }
    else
    {
      Console.WriteLine("ERROR: No se puede conectar a la base de datos");
    }
  }
  catch (Exception ex)
  {
    Console.WriteLine($"ERROR CRÍTICO con base de datos: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");

    // Mostrar variables de entorno para debugging (sin contraseñas)
    Console.WriteLine("Variables de entorno MySQL:");
    Console.WriteLine($"MYSQLHOST: {Environment.GetEnvironmentVariable("MYSQLHOST")}");
    Console.WriteLine($"MYSQLPORT: {Environment.GetEnvironmentVariable("MYSQLPORT")}");
    Console.WriteLine($"MYSQLDATABASE: {Environment.GetEnvironmentVariable("MYSQLDATABASE")}");
    Console.WriteLine($"MYSQLUSER: {Environment.GetEnvironmentVariable("MYSQLUSER")}");
    Console.WriteLine($"MYSQLPASSWORD: {(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLPASSWORD")) ? "NO CONFIGURADA" : "CONFIGURADA")}");

    // En producción, mostrar el error pero continuar para poder diagnosticar
    if (app.Environment.IsProduction())
    {
      Console.WriteLine("CONTINUANDO SIN BASE DE DATOS - ESTO CAUSARÁ ERRORES 500 EN ENDPOINTS QUE USEN BD");
    }
    else
    {
      throw;
    }
  }
}

// 10. Configuración de middleware CON MEJOR MANEJO DE ERRORES
if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}
else
{
  // En producción, usar manejo de errores personalizado
  app.UseExceptionHandler("/error");
}

// Middleware personalizado para capturar errores 500
app.Use(async (context, next) =>
{
  try
  {
    await next();
  }
  catch (Exception ex)
  {
    Console.WriteLine($"ERROR NO MANEJADO: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");

    context.Response.StatusCode = 500;
    await context.Response.WriteAsync($"Error interno: {ex.Message}");
  }
});

// Endpoint para manejo de errores
app.Map("/error", () => Results.Problem("Ha ocurrido un error interno"));

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

// Endpoint de salud mejorado con más información de debugging
app.MapGet("/health", async (AppDbContext db) => {
  try
  {
    var canConnect = await db.Database.CanConnectAsync();
    var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
    var pendingMigrations = db.Database.GetPendingMigrations().ToList();

    return Results.Ok(new
    {
      Status = canConnect ? "Healthy" : "Database Connection Failed",
      Environment = app.Environment.EnvironmentName,
      Database = new
      {
        Type = !string.IsNullOrEmpty(mysqlUrl) ? "Railway MySQL" : "Local MySQL",
        CanConnect = canConnect,
        AppliedMigrations = appliedMigrations.Count,
        PendingMigrations = pendingMigrations.Count,
        Variables = new
        {
          HasMySQLURL = !string.IsNullOrEmpty(mysqlUrl),
          HasMySQLHOST = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLHOST")),
          HasMySQLDATABASE = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLDATABASE")),
          HasMySQLUSER = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLUSER")),
          HasMySQLPASSWORD = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLPASSWORD"))
        }
      },
      JWT = new
      {
        HasKey = !string.IsNullOrEmpty(jwtKey),
        Issuer = jwtIssuer,
        Audience = jwtAudience
      },
      Port = port,
      CorsOrigins = corsOrigins,
      RailwayUrl = railwayUrl,
      Timestamp = DateTime.UtcNow
    });
  }
  catch (Exception ex)
  {
    return Results.Ok(new
    {
      Status = "Unhealthy",
      Error = ex.Message,
      Environment = app.Environment.EnvironmentName,
      Database = new
      {
        Variables = new
        {
          HasMySQLURL = !string.IsNullOrEmpty(mysqlUrl),
          MySQLHOST = Environment.GetEnvironmentVariable("MYSQLHOST"),
          MySQLPORT = Environment.GetEnvironmentVariable("MYSQLPORT"),
          MySQLDATABASE = Environment.GetEnvironmentVariable("MYSQLDATABASE"),
          MySQLUSER = Environment.GetEnvironmentVariable("MYSQLUSER"),
          HasMySQLPASSWORD = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYSQLPASSWORD"))
        }
      },
      Timestamp = DateTime.UtcNow
    });
  }
});

// Endpoint específico para verificar CORS
app.MapGet("/cors-test", () => new { Message = "CORS está funcionando!" });

Console.WriteLine($"Aplicación iniciando en puerto: {port}");
Console.WriteLine($"Entorno: {app.Environment.EnvironmentName}");
Console.WriteLine($"Base de datos: {(!string.IsNullOrEmpty(mysqlUrl) ? "Railway" : "Local")}");
Console.WriteLine($"CORS Origins configurados: {string.Join(", ", corsOrigins)}");

app.Run();
