using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TRKRUN.Context;
using TRKRUN.Identity;
using TRKRUN.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------
// 1. OBTENER EL PUERTO DE RAILWAY O DEFAULT
// ----------------------------------------
var port = Environment.GetEnvironmentVariable("PORT") ?? "80";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


// -------------------------------------------------
// 2. CONFIGURACIÓN DE CORS
//    Creamos una lista de orígenes permitidos y 
//    agregamos dinámicamente el dominio de Railway.
// -------------------------------------------------
var corsOrigins = new List<string>
{
    "http://localhost:4200",
    "http://192.168.1.134:4200",
    "http://192.168.56.1:4200",
    "https://trkrun.netlify.app",
    "https://norman-main-velvet-ticket.trycloudflare.com"
};

// Agregar el dominio público de Railway si existe la variable
var railwayUrl = Environment.GetEnvironmentVariable("RAILWAY_PUBLIC_DOMAIN");
if (!string.IsNullOrEmpty(railwayUrl))
{
  // Por ejemplo: "fronttrkrun-production.up.railway.app"
  corsOrigins.Add($"https://{railwayUrl}");
}

// Registrar la política de CORS
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAngularClient", policy =>
  {
    policy
        .WithOrigins(corsOrigins.ToArray())
        .AllowAnyHeader()
        .AllowAnyMethod();
    // Si en algún punto necesitas credenciales (cookies, etc.), puedes habilitar AllowCredentials(),
    // pero recuerda que con AllowAnyOrigin() y AllowCredentials() al mismo tiempo no funciona:
    // policy.AllowCredentials();
  });
});


// -----------------------------------
// 3. REGISTRAR CONTROLADORES Y SWAGGER
// -----------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ---------------------------------------
// 4. REGISTRAR AUTORIZACIÓN (JWT) Y AUTH
// ---------------------------------------
builder.Services.AddAuthorization();


// ------------------------------------------------------
// 5. CONFIGURAR BASE DE DATOS (MySQL local vs Railway MySQL)
// ------------------------------------------------------
string connectionString;
var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");

if (!string.IsNullOrEmpty(mysqlUrl))
{
  // Estamos en Railway: convierte la URL de conexión
  // Ejemplo: mysql://user:pass@host:port/database
  var uri = new Uri(mysqlUrl);
  var userInfo = uri.UserInfo.Split(':');
  var user = userInfo[0];
  var password = userInfo[1];
  var host = uri.Host;
  var portNumber = uri.Port;
  var database = uri.AbsolutePath.TrimStart('/');

  connectionString = $"Server={host};Port={portNumber};Database={database};Uid={user};Pwd={password};";
}
else
{
  // Desarrollo local: usa appsettings.json
  connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// Registrar el contexto de EF Core (MySQL)
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);


// ------------------------------------------------
// 6. REGISTRAR IDENTITY, HASHPASSWORD Y AUTH SERVICE
// ------------------------------------------------
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();


// ----------------------------------------------------
// 7. REGISTRAR DEMÁS SERVICIOS PERSONALIZADOS (User, etc.)
// ----------------------------------------------------
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITorneoService, TorneoService>();
builder.Services.AddScoped<ICircuitoService, CircuitoService>();
builder.Services.AddScoped<IRolService, RolService>();


// ---------------------------------------
// 8. CONFIGURAR JWT (Key, Issuer, Audience)
// ---------------------------------------
string jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];
string jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"];
string jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"];

// Validar que la clave exista
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
  // En producción, exigir HTTPS; en local puede ser false
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


// ------------------------------------------
// 9. CONSTRUIR LA APLICACIÓN (después de DI / servicios)
// ------------------------------------------
var app = builder.Build();


// ---------------------------------
// 10. APLICAR MIGRACIONES AUTOMÁTICAS
// ---------------------------------
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
    // En producción podrías querer detener la app si falla la migración
    if (app.Environment.IsProduction())
    {
      throw;
    }
  }
}


// ------------------------------------------------------
// 11. CONFIGURAR MIDDLEWARES: ERRORES, HTTPS, CORS, AUTH
// ------------------------------------------------------
if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}

// Habilitar Swagger siempre (o condicional en dev)
app.UseSwagger();
app.UseSwaggerUI();

// No aplicar HTTPS redirect en producción en Railway (Railway ya maneja TLS)
if (!app.Environment.IsProduction())
{
  app.UseHttpsRedirection();
}

// Aplicar la política de CORS justo antes de autenticación/autorización
app.UseCors("AllowAngularClient");

// Aplicar autenticación y autorización (JWT)
app.UseAuthentication();
app.UseAuthorization();

// Mapear controladores (API endpoints)
app.MapControllers();


// -------------------------------------------
// 12. ENDPOINT DE SALUD (para comprobar status)
// -------------------------------------------
app.MapGet("/health", () => new
{
  Status = "Healthy",
  Environment = app.Environment.EnvironmentName,
  Database = !string.IsNullOrEmpty(mysqlUrl) ? "Railway MySQL" : "Local MySQL",
  HasJwtKey = !string.IsNullOrEmpty(jwtKey),
  Port = port,
  Timestamp = DateTime.UtcNow
});

Console.WriteLine($"Aplicación iniciando en puerto: {port}");
Console.WriteLine($"Entorno: {app.Environment.EnvironmentName}");
Console.WriteLine($"Base de datos: {(!string.IsNullOrEmpty(mysqlUrl) ? "Railway" : "Local")}");

// Arrancar la aplicación
app.Run();
