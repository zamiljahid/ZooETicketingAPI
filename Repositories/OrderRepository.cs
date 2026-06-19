using Dapper;
using Npgsql;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;

namespace ZooTicketing.API.Repositories;

public class OrderRepository(IConfiguration cfg) : BaseRepository(cfg), IOrderRepository
{
    static OrderSummary MapSum(dynamic r) => new((int)r.id,(string)r.issued_channel,(decimal)r.total_amount,(string)r.payment_status,(string)r.payment_method,(DateTime)r.created_at,(int)(long)r.ticket_count);

    public async Task<IEnumerable<OrderSummary>> GetAllAsync() {
        using var db = Conn();
        return (await db.QueryAsync<dynamic>("""
            SELECT o.id,o.issued_channel,o.total_amount,o.payment_status,o.payment_method,o.created_at,COUNT(t.id) AS ticket_count
            FROM orders o LEFT JOIN tickets t ON t.order_id=o.id GROUP BY o.id ORDER BY o.created_at DESC
        """)).Select(MapSum);
    }

    public async Task<IEnumerable<OrderSummary>> GetByUserAsync(int userId) {
        if(userId<=0) throw new InvalidOperationException("Invalid user ID.");
        using var db = Conn();
        return (await db.QueryAsync<dynamic>("""
            SELECT o.id,o.issued_channel,o.total_amount,o.payment_status,o.payment_method,o.created_at,COUNT(t.id) AS ticket_count
            FROM orders o LEFT JOIN tickets t ON t.order_id=o.id WHERE o.user_id=@userId GROUP BY o.id ORDER BY o.created_at DESC
        """,new{userId})).Select(MapSum);
    }

    public async Task<OrderDetail?> GetByIdAsync(int id) {
        using var db = Conn();
        var o = await db.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT o.*,u.name AS customer_name FROM orders o LEFT JOIN users u ON u.id=o.user_id WHERE o.id=@id
        """,new{id});
        if(o is null) return null;
        var tickets = (await db.QueryAsync<dynamic>("""
            SELECT t.id,t.ticket_token,t.holder_name,tt.name AS type,t.price_paid,t.visit_date,t.status,t.issued_channel
            FROM tickets t JOIN ticket_types tt ON tt.id=t.ticket_type_id WHERE t.order_id=@id
        """,new{id})).Select(t=>new TicketDto((int)t.id,(Guid)t.ticket_token,
            t.holder_name is null?"Guest":(string)t.holder_name,(string)t.type,(decimal)t.price_paid,
            DateOnly.FromDateTime((DateTime)t.visit_date),(string)t.status,(string)t.issued_channel)).ToList();
        return new((int)o.id,o.customer_name is null?null:(string?)o.customer_name,
            o.guest_name is null?null:(string?)o.guest_name,o.guest_phone is null?null:(string?)o.guest_phone,
            (string)o.issued_channel,(decimal)o.total_amount,(string)o.payment_status,
            (string)o.payment_method,(DateTime)o.created_at,tickets);
    }

    public async Task<OrderDetail> CreateAsync(CreateOrderRequest req, int? issuedByUserId)
    {
        // Input validation
        if(req.Tickets==null||req.Tickets.Count==0)
            throw new InvalidOperationException("At least one ticket is required.");
        if(req.VisitDate < DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidOperationException("Visit date cannot be in the past.");
        if(string.IsNullOrWhiteSpace(req.IssuedChannel))
            throw new InvalidOperationException("Issued channel is required.");
        if(req.IssuedChannel=="counter" && string.IsNullOrWhiteSpace(req.GuestName) && req.UserId==null)
            throw new InvalidOperationException("Guest name or user ID is required for counter orders.");

        using var db = Conn();
        await db.OpenAsync();
        await using var tx = await db.BeginTransactionAsync();
        try {
            // Sales control check
            var ctrl = await db.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT sales_open,capacity FROM sales_control WHERE control_date=@date",
                new{date=req.VisitDate},tx);
            if(ctrl!=null) {
                if(!(bool)ctrl.sales_open)
                    throw new InvalidOperationException("Ticket sales are closed for this date.");
                if(ctrl.capacity is not null) {
                    var sold = await db.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM tickets WHERE visit_date=@date AND status!='cancelled'",
                        new{date=req.VisitDate},tx);
                    if(sold+req.Tickets.Count>(int)ctrl.capacity)
                        throw new InvalidOperationException($"Not enough capacity. Only {(int)ctrl.capacity - sold} ticket(s) remaining for this date.");
                }
            }

            // Validate all ticket type IDs exist
            var prices = (await db.QueryAsync<dynamic>("SELECT id,price FROM ticket_types",transaction:tx))
                .ToDictionary(r=>(int)r.id,r=>(decimal)r.price);
            foreach(var line in req.Tickets)
                if(!prices.ContainsKey(line.TicketTypeId))
                    throw new InvalidOperationException($"Ticket type ID {line.TicketTypeId} does not exist.");

            decimal total = req.Tickets.Sum(t=>prices[t.TicketTypeId]);

            var orderId = await db.ExecuteScalarAsync<int>("""
                INSERT INTO orders(user_id,issued_by_user_id,guest_name,guest_phone,issued_channel,total_amount,payment_status,payment_method)
                VALUES(@UserId,@IssuedBy,@GuestName,@GuestPhone,@Channel::order_channel,@Total,'paid'::payment_status,@PayMethod::payment_method)
                RETURNING id
            """,new{UserId=req.UserId,IssuedBy=issuedByUserId,req.GuestName,req.GuestPhone,
                Channel=req.IssuedChannel,Total=total,PayMethod=req.PaymentMethod},tx);

            var ticketDtos = new List<TicketDto>();
            foreach(var line in req.Tickets) {
                var tok = Guid.NewGuid();
                var tid = await db.ExecuteScalarAsync<int>("""
                    INSERT INTO tickets(order_id,ticket_type_id,ticket_token,holder_name,visit_date,price_paid,status,issued_channel)
                    VALUES(@OrderId,@TypeId,@Token,@Holder,@VisitDate,@Price,'valid'::ticket_status,@Channel::order_channel)
                    RETURNING id
                """,new{OrderId=orderId,TypeId=line.TicketTypeId,Token=tok,Holder=line.HolderName,
                    VisitDate=req.VisitDate,Price=prices[line.TicketTypeId],Channel=req.IssuedChannel},tx);
                var typeName = await db.ExecuteScalarAsync<string>("SELECT name FROM ticket_types WHERE id=@id",new{id=line.TicketTypeId},tx)??"";
                ticketDtos.Add(new(tid,tok,line.HolderName,typeName,prices[line.TicketTypeId],req.VisitDate,"valid",req.IssuedChannel));
            }
            await tx.CommitAsync();
            return new(orderId,null,req.GuestName,req.GuestPhone,req.IssuedChannel,total,"paid",req.PaymentMethod,DateTime.UtcNow,ticketDtos);
        }
        catch(PostgresException ex) {
            await tx.RollbackAsync();
            throw new InvalidOperationException($"Database error while creating order: {ex.MessageText}");
        }
        catch {
            await tx.RollbackAsync();
            throw;
        }
    }
}
