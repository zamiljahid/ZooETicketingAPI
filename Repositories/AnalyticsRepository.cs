using Dapper;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;

namespace ZooTicketing.API.Repositories;

public class AnalyticsRepository(IConfiguration cfg) : BaseRepository(cfg), IAnalyticsRepository
{
    public async Task<IEnumerable<DailySales>> GetDailySalesAsync(DateOnly? from, DateOnly? to)
    {
        using var db = Conn();
        return (await db.QueryAsync<dynamic>("""
            SELECT t.visit_date AS date,tt.name AS ticket_type,t.issued_channel AS channel,
                   COUNT(t.id) AS count,SUM(t.price_paid) AS revenue
            FROM tickets t JOIN ticket_types tt ON tt.id=t.ticket_type_id
            WHERE t.status!='cancelled'
              AND (@from::date IS NULL OR t.visit_date>=@from)
              AND (@to::date   IS NULL OR t.visit_date<=@to)
            GROUP BY t.visit_date,tt.name,t.issued_channel
            ORDER BY t.visit_date DESC
        """,new{from,to})).Select(r=>new DailySales(
            DateOnly.FromDateTime((DateTime)r.date),(string)r.ticket_type,
            (string)r.channel,(int)(long)r.count,(decimal)r.revenue));
    }

    public async Task<IEnumerable<GateActivity>> GetGateActivityAsync(DateOnly? date)
    {
        using var db = Conn();
        return (await db.QueryAsync<dynamic>("""
            SELECT g.name AS gate,sl.scan_result AS result,COUNT(*) AS total,DATE(sl.scanned_at) AS date
            FROM scan_logs sl JOIN gates g ON g.id=sl.gate_id
            WHERE (@date::date IS NULL OR DATE(sl.scanned_at)=@date)
            GROUP BY g.name,sl.scan_result,DATE(sl.scanned_at)
            ORDER BY date DESC,total DESC
        """,new{date})).Select(r=>new GateActivity((string)r.gate,(string)r.result,(int)(long)r.total,DateOnly.FromDateTime((DateTime)r.date)));
    }

    public async Task<CapacityStatus?> GetCapacityAsync(DateOnly date)
    {
        using var db = Conn();
        var r = await db.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT sc.control_date,sc.sales_open,sc.capacity,COUNT(t.id) AS sold
            FROM sales_control sc
            LEFT JOIN tickets t ON t.visit_date=sc.control_date AND t.status!='cancelled'
            WHERE sc.control_date=@date GROUP BY sc.control_date,sc.sales_open,sc.capacity
        """,new{date});
        if(r is null) return null;
        int sold=(int)(long)r.sold; int? cap=r.capacity is null?null:(int?)r.capacity;
        return new(DateOnly.FromDateTime((DateTime)r.control_date),(bool)r.sales_open,cap,sold,cap.HasValue?cap-sold:null);
    }
}
