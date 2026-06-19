using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ZooTicketing.API.Data;
using ZooTicketing.API.Interfaces;
using ZooTicketing.API.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ── Dapper: teach it DateOnly / TimeOnly (no native support in 2.1.x) ──
SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
SqlMapper.AddTypeHandler(new TimeOnlyTypeHandler());

// ── CORS ──────────────────────────────────────────────────────
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── EF Core (Npgsql) ──────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Dapper repositories (DI) ──────────────────────────────────
builder.Services.AddScoped<IAuthRepository,         AuthRepository>();
builder.Services.AddScoped<ITicketTypeRepository,   TicketTypeRepository>();
builder.Services.AddScoped<IGateRepository,         GateRepository>();
builder.Services.AddScoped<ISalesControlRepository, SalesControlRepository>();
builder.Services.AddScoped<IOrderRepository,        OrderRepository>();
builder.Services.AddScoped<ITicketRepository,       TicketRepository>();
builder.Services.AddScoped<IScanRepository,         ScanRepository>();
builder.Services.AddScoped<IAnalyticsRepository,    AnalyticsRepository>();

// ── JWT Auth ──────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    });
builder.Services.AddAuthorization();

// ── Swagger ───────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Zoo Ticketing API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "Authorization", Type = SecuritySchemeType.Http,
        Scheme = "Bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {{
        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type=ReferenceType.SecurityScheme, Id="Bearer" }},
        Array.Empty<string>()
    }});
});

// ── JSON: DateOnly support ─────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

app.UseCors("AllowAll");
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zoo Ticketing API v1"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
