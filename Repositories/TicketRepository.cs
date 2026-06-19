using Dapper;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;

namespace ZooTicketing.API.Repositories;

public class TicketRepository(IConfiguration cfg) : BaseRepository(cfg), ITicketRepository
{
    const string Sel = """
        SELECT t.id,t.ticket_token,t.holder_name,tt.name AS type,t.price_paid,t.visit_date,t.status,t.issued_channel
        FROM tickets t JOIN ticket_types tt ON tt.id=t.ticket_type_id
    """;
    static TicketDto Map(dynamic t) => new((int)t.id,(Guid)t.ticket_token,
        t.holder_name is null?"Guest":(string)t.holder_name,(string)t.type,(decimal)t.price_paid,
        DateOnly.FromDateTime((DateTime)t.visit_date),(string)t.status,(string)t.issued_channel);

    public async Task<TicketDto?> GetByIdAsync(int id) {
        if(id<=0) throw new InvalidOperationException("Invalid ticket ID.");
        using var db = Conn();
        var r = await db.QuerySingleOrDefaultAsync<dynamic>($"{Sel} WHERE t.id=@id",new{id});
        return r is null?null:Map(r);
    }

    public async Task<TicketDto?> GetByTokenAsync(Guid token) {
        if(token==Guid.Empty) throw new InvalidOperationException("Invalid ticket token.");
        using var db = Conn();
        var r = await db.QuerySingleOrDefaultAsync<dynamic>($"{Sel} WHERE t.ticket_token=@token",new{token});
        return r is null?null:Map(r);
    }

    public async Task<bool> CancelAsync(int id) {
        if(id<=0) throw new InvalidOperationException("Invalid ticket ID.");
        using var db = Conn();
        var ticket = await db.QuerySingleOrDefaultAsync<dynamic>("SELECT status FROM tickets WHERE id=@id",new{id});
        if(ticket is null)          throw new InvalidOperationException("Ticket not found.");
        if((string)ticket.status=="used")      throw new InvalidOperationException("Cannot cancel a ticket that has already been scanned.");
        if((string)ticket.status=="cancelled") throw new InvalidOperationException("Ticket is already cancelled.");
        return await db.ExecuteAsync("UPDATE tickets SET status='cancelled' WHERE id=@id",new{id}) > 0;
    }
}
