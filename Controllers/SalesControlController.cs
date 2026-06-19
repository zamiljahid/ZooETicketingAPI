using Microsoft.AspNetCore.Mvc;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;
namespace ZooTicketing.API.Controllers;
[ApiController][Route("api/sales-control")]
public class SalesControlController(ISalesControlRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() {
        try { return Ok(await repo.GetAllAsync()); }
        catch(Exception ex){ return StatusCode(500,$"Failed to fetch sales control records: {ex.Message}"); }
    }

    [HttpGet("today/capacity")]
    public async Task<IActionResult> TodayCapacity() {
        try {
            var r=await repo.GetTodayCapacityAsync();
            return r is null ? NotFound("No sales control record found for today.") : Ok(r);
        }
        catch(Exception ex){ return StatusCode(500,$"Error: {ex.Message}"); }
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> GetByDate(DateOnly date) {
        try {
            var r=await repo.GetByDateAsync(date);
            return r is null ? NotFound($"No record found for {date}.") : Ok(r);
        }
        catch(Exception ex){ return StatusCode(500,$"Error: {ex.Message}"); }
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(SalesControlRequest req) {
        try {
            if(req.Capacity.HasValue&&req.Capacity<=0) return BadRequest("Capacity must be greater than zero.");
            return Ok(await repo.UpsertAsync(req,1)); // TODO: extract admin ID from JWT
        }
        catch(InvalidOperationException ex){ return BadRequest(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Failed to save sales control: {ex.Message}"); }
    }

    [HttpPatch("{date}/toggle")]
    public async Task<IActionResult> Toggle(DateOnly date,[FromBody]bool open) {
        try {
            return await repo.ToggleSalesAsync(date,open,1) ? Ok(open?"Sales opened.":"Sales closed.") : BadRequest("Failed to toggle sales.");
        }
        catch(Exception ex){ return StatusCode(500,$"Failed to toggle sales: {ex.Message}"); }
    }
}
