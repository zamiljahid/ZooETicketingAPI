using Microsoft.AspNetCore.Mvc;
using ZooTicketing.API.Interfaces;
namespace ZooTicketing.API.Controllers;
[ApiController][Route("api/analytics")]
public class AnalyticsController(IAnalyticsRepository repo) : ControllerBase
{
    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery]DateOnly? from,[FromQuery]DateOnly? to) {
        try {
            if(from.HasValue&&to.HasValue&&from>to) return BadRequest("'from' date cannot be after 'to' date.");
            return Ok(await repo.GetDailySalesAsync(from,to));
        }
        catch(Exception ex){ return StatusCode(500,$"Failed to fetch sales data: {ex.Message}"); }
    }

    [HttpGet("gates")]
    public async Task<IActionResult> Gates([FromQuery]DateOnly? date) {
        try { return Ok(await repo.GetGateActivityAsync(date)); }
        catch(Exception ex){ return StatusCode(500,$"Failed to fetch gate activity: {ex.Message}"); }
    }

    [HttpGet("capacity")]
    public async Task<IActionResult> Capacity([FromQuery]DateOnly date) {
        try {
            var r=await repo.GetCapacityAsync(date);
            return r is null ? NotFound($"No capacity record for {date}.") : Ok(r);
        }
        catch(Exception ex){ return StatusCode(500,$"Failed to fetch capacity: {ex.Message}"); }
    }
}
