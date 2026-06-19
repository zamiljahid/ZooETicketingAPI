using Dapper;
using Npgsql;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;

namespace ZooTicketing.API.Repositories;

public class TicketTypeRepository(IConfiguration cfg) : BaseRepository(cfg), ITicketTypeRepository
{
    static TicketTypeDto Map(dynamic r) => new((int)r.id,(string)r.name,(decimal)r.price);

    public async Task<IEnumerable<TicketTypeDto>> GetAllAsync() {
        using var db = Conn();
        return (await db.QueryAsync<dynamic>("SELECT id,name,price FROM ticket_types ORDER BY id")).Select(Map);
    }
    public async Task<TicketTypeDto?> GetByIdAsync(int id) {
        using var db = Conn();
        var r = await db.QuerySingleOrDefaultAsync<dynamic>("SELECT id,name,price FROM ticket_types WHERE id=@id",new{id});
        return r is null ? null : Map(r);
    }
    public async Task<TicketTypeDto> CreateAsync(TicketTypeRequest req) {
        using var db = Conn();
        try {
            var id = await db.ExecuteScalarAsync<int>("INSERT INTO ticket_types(name,price) VALUES(@Name,@Price) RETURNING id",req);
            return new(id,req.Name,req.Price);
        }
        catch(PostgresException ex) when(ex.SqlState=="23505") {
            throw new InvalidOperationException($"Ticket type '{req.Name}' already exists.");
        }
    }
    public async Task<bool> UpdateAsync(int id, TicketTypeRequest req) {
        if(req.Price < 0) throw new InvalidOperationException("Price cannot be negative.");
        using var db = Conn();
        try {
            return await db.ExecuteAsync("UPDATE ticket_types SET name=@Name,price=@Price WHERE id=@id",new{req.Name,req.Price,id}) > 0;
        }
        catch(PostgresException ex) when(ex.SqlState=="23505") {
            throw new InvalidOperationException($"Ticket type '{req.Name}' already exists.");
        }
    }
    public async Task<bool> DeleteAsync(int id) {
        using var db = Conn();
        try {
            return await db.ExecuteAsync("DELETE FROM ticket_types WHERE id=@id",new{id}) > 0;
        }
        catch(PostgresException ex) when(ex.SqlState=="23503") {
            throw new InvalidOperationException("Cannot delete ticket type — it is used by existing tickets.");
        }
    }
}
