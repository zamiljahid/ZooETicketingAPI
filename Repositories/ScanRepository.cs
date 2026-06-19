using Dapper;
using Npgsql;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;

namespace ZooTicketing.API.Repositories;

public class ScanRepository(IConfiguration cfg) : BaseRepository(cfg), IScanRepository
{
    private static TimeOnly ToTimeOnly(object v) => v switch
    {
        TimeSpan ts => TimeOnly.FromTimeSpan(ts),
        TimeOnly t  => t,
        DateTime dt => TimeOnly.FromDateTime(dt),
        _           => TimeOnly.Parse(v.ToString()!)
    };

    public async Task<ScanResponse> ScanAsync(ScanRequest req)
    {
        if(string.IsNullOrWhiteSpace(req.TicketToken))
            return new(false,"invalid_token",null,null,null,null,"Ticket token is required.");
        if(!Guid.TryParse(req.TicketToken,out var token))
            return new(false,"invalid_token",null,null,null,null,"Invalid QR code format.");
        if(req.GateId<=0)
            return new(false,"invalid_token",null,null,null,null,"Invalid gate ID.");

        using var db = Conn();
        await db.OpenAsync();

        // 1. Validate gate
        var gate = await db.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT id,name,is_active,open_time,close_time FROM gates WHERE id=@id",new{id=req.GateId});
        if(gate is null)
            return new(false,"invalid_token",null,null,null,null,"Gate not found.");
        if(!(bool)gate.is_active)
            return new(false,"gate_inactive",null,null,null,(string)gate.name,"This gate is currently closed.");

        // 2. Validate gate hours
        // Npgsql returns a `time` column as a boxed TimeSpan on a dynamic row, which
        // cannot be unbox-cast straight to TimeOnly — convert defensively.
        var now   = TimeOnly.FromDateTime(DateTime.Now);
        var open  = ToTimeOnly((object)gate.open_time);
        var close = ToTimeOnly((object)gate.close_time);
        if(now<open||now>close)
            return new(false,"outside_hours",null,null,null,(string)gate.name,$"Gate is only open between {open:HH\\:mm} and {close:HH\\:mm}.");

        // 3. Validate ticket
        var t = await db.QuerySingleOrDefaultAsync<dynamic>("""
            SELECT t.id,t.status,t.visit_date,t.holder_name,tt.name AS type
            FROM tickets t JOIN ticket_types tt ON tt.id=t.ticket_type_id
            WHERE t.ticket_token=@token
        """,new{token});

        string result; bool allowed=false; string msg;
        if(t is null) {
            result="invalid_token"; msg="Ticket not found. Please check the QR code.";
        } else {
            var visitDate = DateOnly.FromDateTime((DateTime)t.visit_date);
            var today     = DateOnly.FromDateTime(DateTime.Today);
            var status    = (string)t.status;

            if(status=="used")        { result="already_used"; msg="This ticket has already been scanned. Re-entry is not allowed."; }
            else if(status=="cancelled") { result="cancelled";    msg="This ticket has been cancelled."; }
            else if(status=="expired")   { result="invalid_token"; msg="This ticket has expired."; }
            else if(status!="valid")     { result="invalid_token"; msg=$"Ticket status is '{status}' and cannot be used."; }
            else if(visitDate<today)     { result="wrong_date";    msg=$"This ticket was valid for {visitDate:dd MMM yyyy}. It has expired."; }
            else if(visitDate>today)     { result="wrong_date";    msg=$"This ticket is valid for {visitDate:dd MMM yyyy}, not today."; }
            else {
                // All checks passed — atomically mark as used
                var updated = await db.ExecuteAsync(
                    "UPDATE tickets SET status='used' WHERE id=@id AND status='valid'",new{id=(int)t.id});
                if(updated==0) {
                    result="already_used"; msg="Ticket was just used by another scan.";
                } else {
                    result="allowed"; allowed=true; msg="Entry granted. Welcome to the zoo! 🦁";
                }
            }
        }

        // 4. Always log the scan attempt
        try {
            await db.ExecuteAsync("""
                INSERT INTO scan_logs(ticket_id,gate_id,scanned_by,scan_result,scanned_at)
                VALUES(@TicketId,@GateId,@ScannedBy,@Result::scan_result,NOW())
            """,new{TicketId=t is null?0:(int)t.id,req.GateId,req.ScannedBy,Result=result});
        }
        catch(PostgresException) { /* log failure should never break the scan response */ }

        return new(allowed,result,
            t is null?null:(t.holder_name is null?null:(string?)t.holder_name),
            t is null?null:(string?)t.type,
            t is null?null:DateOnly.FromDateTime((DateTime)t.visit_date),
            (string)gate.name,msg);
    }

    public async Task<IEnumerable<GateActivity>> GetActivityAsync(DateOnly? date) {
        using var db = Conn();
        return (await db.QueryAsync<dynamic>("""
            SELECT g.name AS gate,sl.scan_result AS result,COUNT(*) AS total,DATE(sl.scanned_at) AS date
            FROM scan_logs sl JOIN gates g ON g.id=sl.gate_id
            WHERE (@date::date IS NULL OR DATE(sl.scanned_at)=@date)
            GROUP BY g.name,sl.scan_result,DATE(sl.scanned_at)
            ORDER BY date DESC,total DESC
        """,new{date})).Select(r=>new GateActivity((string)r.gate,(string)r.result,(int)(long)r.total,DateOnly.FromDateTime((DateTime)r.date)));
    }
}
