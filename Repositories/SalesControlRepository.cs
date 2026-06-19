using Dapper;
using Npgsql;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;

namespace ZooTicketing.API.Repositories;

public class SalesControlRepository(IConfiguration cfg) : BaseRepository(cfg), ISalesControlRepository
{
    static SalesControlDto Map(dynamic r) => new((int)r.id,
        DateOnly.FromDateTime((DateTime)r.control_date),(bool)r.sales_open,
        r.capacity is null?null:(int?)r.capacity,(DateTime)r.updated_at);

    public async Task<IEnumerable<SalesControlDto>> GetAllAsync() {
        using var db = Conn();
        return (await db.QueryAsync<dynamic>("SELECT id,control_date,sales_open,capacity,updated_at FROM sales_control ORDER BY control_date DESC")).Select(Map);
    }

    public async Task<SalesControlDto?> GetByDateAsync(DateOnly date) {
        using var db = Conn();
        var r = await db.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT id,control_date,sales_open,capacity,updated_at FROM sales_control WHERE control_date=@date",new{date});
        return r is null?null:Map(r);
    }

    public async Task<SalesControlDto> UpsertAsync(SalesControlRequest req, int adminId) {
        if(req.Capacity.HasValue && req.Capacity<=0)
            throw new InvalidOperationException("Capacity must be greater than zero.");
        using var db = Conn();
        var r = await db.QuerySingleAsync<dynamic>("""
            INSERT INTO sales_control(control_date,sales_open,capacity,updated_by,updated_at)
            VALUES(@ControlDate,@SalesOpen,@Capacity,@adminId,NOW())
            ON CONFLICT(control_date) DO UPDATE
              SET sales_open=EXCLUDED.sales_open,capacity=EXCLUDED.capacity,
                  updated_by=EXCLUDED.updated_by,updated_at=NOW()
            RETURNING id,control_date,sales_open,capacity,updated_at
        """,new{req.ControlDate,req.SalesOpen,req.Capacity,adminId});
        return Map(r);
    }

    public async Task<bool> ToggleSalesAsync(DateOnly date, bool open, int adminId) {
        using var db = Conn();
        return await db.ExecuteAsync("""
            INSERT INTO sales_control(control_date,sales_open,updated_by,updated_at)
            VALUES(@date,@open,@adminId,NOW())
            ON CONFLICT(control_date) DO UPDATE
              SET sales_open=EXCLUDED.sales_open,updated_by=EXCLUDED.updated_by,updated_at=NOW()
        """,new{date,open,adminId}) > 0;
    }

    public async Task<CapacityStatus?> GetTodayCapacityAsync() {
        using var db = Conn();
        var r = await db.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT sc.control_date,sc.sales_open,sc.capacity,COUNT(t.id) AS sold
            FROM sales_control sc
            LEFT JOIN tickets t ON t.visit_date=sc.control_date AND t.status!='cancelled'
            WHERE sc.control_date=CURRENT_DATE
            GROUP BY sc.control_date,sc.sales_open,sc.capacity
        """);
        if(r is null) return null;
        int sold=(int)(long)r.sold; int? cap=r.capacity is null?null:(int?)r.capacity;
        return new(DateOnly.FromDateTime((DateTime)r.control_date),(bool)r.sales_open,cap,sold,cap.HasValue?cap-sold:null);
    }
}
