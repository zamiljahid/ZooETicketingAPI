using Dapper;
using Npgsql;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;

namespace ZooTicketing.API.Repositories;

public class GateRepository(IConfiguration cfg) : BaseRepository(cfg), IGateRepository
{
    static GateDto Map(dynamic r) => new((int)r.id,(string)r.name,(bool)r.is_active,r.open_time.ToString(),r.close_time.ToString());

    public async Task<IEnumerable<GateDto>> GetAllAsync() {
        using var db = Conn();
        return (await db.QueryAsync<dynamic>("SELECT id,name,is_active,open_time,close_time FROM gates ORDER BY id")).Select(Map);
    }
    public async Task<GateDto?> GetByIdAsync(int id) {
        using var db = Conn();
        var r = await db.QuerySingleOrDefaultAsync<dynamic>("SELECT id,name,is_active,open_time,close_time FROM gates WHERE id=@id",new{id});
        return r is null ? null : Map(r);
    }
    public async Task<GateDto> CreateAsync(GateRequest req) {
        using var db = Conn();
        try {
            var id = await db.ExecuteScalarAsync<int>(
                "INSERT INTO gates(name,is_active,open_time,close_time) VALUES(@Name,@IsActive,@OpenTime::time,@CloseTime::time) RETURNING id",
                new{req.Name,req.IsActive,req.OpenTime,req.CloseTime});
            return new(id,req.Name,req.IsActive,req.OpenTime,req.CloseTime);
        }
        catch(PostgresException ex) when(ex.SqlState=="23505") {
            throw new InvalidOperationException($"A gate named '{req.Name}' already exists.");
        }
    }
    public async Task<bool> UpdateAsync(int id, GateRequest req) {
        using var db = Conn();
        try {
            return await db.ExecuteAsync(
                "UPDATE gates SET name=@Name,is_active=@IsActive,open_time=@OpenTime::time,close_time=@CloseTime::time WHERE id=@id",
                new{req.Name,req.IsActive,req.OpenTime,req.CloseTime,id}) > 0;
        }
        catch(PostgresException ex) when(ex.SqlState=="23505") {
            throw new InvalidOperationException($"A gate named '{req.Name}' already exists.");
        }
    }
    public async Task<bool> ToggleActiveAsync(int id, bool isActive) {
        using var db = Conn();
        return await db.ExecuteAsync("UPDATE gates SET is_active=@isActive WHERE id=@id",new{isActive,id}) > 0;
    }
    public async Task<bool> DeleteAsync(int id) {
        using var db = Conn();
        try {
            return await db.ExecuteAsync("DELETE FROM gates WHERE id=@id",new{id}) > 0;
        }
        catch(PostgresException ex) when(ex.SqlState=="23503") {
            throw new InvalidOperationException("Cannot delete gate — it has existing scan logs.");
        }
    }
}
