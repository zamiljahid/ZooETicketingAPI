using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;

namespace ZooTicketing.API.Repositories;

public class AuthRepository(IConfiguration cfg) : BaseRepository(cfg), IAuthRepository
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest req)
    {
        if(string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            throw new InvalidOperationException("Email and password are required.");

        using var db = Conn();
        var row = await db.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT u.id,u.name,u.email,u.password_hash,r.name AS role
            FROM users u JOIN roles r ON r.id=u.role_id
            WHERE u.email=@Email AND u.is_active=true
        """,new{req.Email});

        if(row==null || (string)row.password_hash != req.Password) return null;

        var token = MakeJwt((int)row.id,(string)row.email,(string)row.role,cfg);
        return new LoginResponse((int)row.id,(string)row.name,(string)row.email,(string)row.role,token);
    }

    public async Task<bool> RegisterAsync(RegisterRequest req)
    {
        if(string.IsNullOrWhiteSpace(req.Name))    throw new InvalidOperationException("Name is required.");
        if(string.IsNullOrWhiteSpace(req.Email))   throw new InvalidOperationException("Email is required.");
        if(string.IsNullOrWhiteSpace(req.Password))throw new InvalidOperationException("Password is required.");
        if(req.Password.Length < 6)                throw new InvalidOperationException("Password must be at least 6 characters.");

        using var db = Conn();
        try {
            var exists = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM users WHERE email=@Email",new{req.Email});
            if(exists > 0) throw new InvalidOperationException("Email already registered.");

            return await db.ExecuteAsync(
                "INSERT INTO users(role_id,name,email,phone,password_hash) VALUES(3,@Name,@Email,@Phone,@Password)",
                new{req.Name,req.Email,req.Phone,Password=req.Password}) > 0;
        }
        catch(PostgresException ex) when(ex.SqlState=="23505") {
            throw new InvalidOperationException("Email already registered.");
        }
    }

    // Counter staff use this to attach a walk-in sale to an existing customer
    // account by typing their phone number (or email).
    public async Task<UserLookupDto?> FindByContactAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query)) return null;
        var q = query.Trim();
        using var db = Conn();
        var row = await db.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT u.id,u.name,u.email,u.phone
            FROM users u
            WHERE u.is_active=true AND (u.phone=@q OR u.email=@q)
            ORDER BY u.id
            LIMIT 1
        """,new{q});
        if(row is null) return null;
        return new UserLookupDto((int)row.id,(string)row.name,(string)row.email,
            row.phone is null ? null : (string?)row.phone);
    }

    public static string MakeJwt(int id, string email, string role, IConfiguration cfg)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier,id.ToString()),
            new Claim(ClaimTypes.Email,email),
            new Claim(ClaimTypes.Role,role)
        };
        var token = new JwtSecurityToken(cfg["Jwt:Issuer"],cfg["Jwt:Audience"],claims,
            expires:DateTime.UtcNow.AddHours(double.Parse(cfg["Jwt:ExpiryHours"]!)),
            signingCredentials:creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
