using Microsoft.AspNetCore.Mvc;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;
namespace ZooTicketing.API.Controllers;
[ApiController][Route("api/scan")]
public class ScanController(IScanRepository repo) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Scan(ScanRequest req) {
        try {
            if(string.IsNullOrWhiteSpace(req.TicketToken)) return BadRequest("Ticket token is required.");
            if(req.GateId<=0) return BadRequest("Valid gate ID is required.");
            return Ok(await repo.ScanAsync(req));
        }
        catch(Exception ex){ return StatusCode(500,$"Scan failed: {ex.Message}"); }
    }

    [HttpGet("activity")]
    public async Task<IActionResult> Activity([FromQuery]DateOnly? date) {
        try { return Ok(await repo.GetActivityAsync(date)); }
        catch(Exception ex){ return StatusCode(500,$"Failed to fetch activity: {ex.Message}"); }
    }
}
