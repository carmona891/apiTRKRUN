using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TRKRUN.Context;
using TRKRUN.Identity;
using TRKRUN.Services;


var builder = WebApplication.CreateBuilder(args);

// 1. Añadir política de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200",
            "http://192.168.1.134:4200",
            "http://192.168.56.1:4200",
            "https://norman-main-velvet-ticket.trycloudflare.com/"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 2. Añadir controladores y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Añadir Authorization (necesario para JWT)
builder.Services.AddAuthorization();

// 4. Configurar DbContext (MySQL)
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
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

// 8. Configurar JWT Authentication
//    - Leer información de la sección "Jwt" en appsettings.json
var jwtSection = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // En producción, poner true
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

var app = builder.Build();

// 9. Pipeline HTTP


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();

// 10. Aplicar CORS ANTES de la autenticación/autorización
app.UseCors("AllowAngularClient");

// 11. Autenticación → Autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
