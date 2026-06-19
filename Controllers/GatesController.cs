using Microsoft.AspNetCore.Mvc;
using ZooTicketing.API.DTOs;
using ZooTicketing.API.Interfaces;
namespace ZooTicketing.API.Controllers;
[ApiController][Route("api/gates")]
public class GatesController(IGateRepository repo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() {
        try { return Ok(await repo.GetAllAsync()); }
        catch(Exception ex){ return StatusCode(500,$"Failed to fetch gates: {ex.Message}"); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id) {
        try {
            if(id<=0) return BadRequest("Invalid ID.");
            var r=await repo.GetByIdAsync(id);
            return r is null ? NotFound($"Gate {id} not found.") : Ok(r);
        }
        catch(Exception ex){ return StatusCode(500,$"Error: {ex.Message}"); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(GateRequest req) {
        try {
            if(string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Gate name is required.");
            if(string.IsNullOrWhiteSpace(req.OpenTime)||string.IsNullOrWhiteSpace(req.CloseTime)) return BadRequest("Open and close times are required.");
            if(!TimeOnly.TryParse(req.OpenTime,out var o)||!TimeOnly.TryParse(req.CloseTime,out var c)) return BadRequest("Invalid time format. Use HH:mm:ss.");
            if(c<=o) return BadRequest("Close time must be after open time.");
            return Ok(await repo.CreateAsync(req));
        }
        catch(InvalidOperationException ex){ return Conflict(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Failed to create gate: {ex.Message}"); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, GateRequest req) {
        try {
            if(id<=0) return BadRequest("Invalid ID.");
            if(string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Gate name is required.");
            if(!TimeOnly.TryParse(req.OpenTime,out var o)||!TimeOnly.TryParse(req.CloseTime,out var c)) return BadRequest("Invalid time format.");
            if(c<=o) return BadRequest("Close time must be after open time.");
            return await repo.UpdateAsync(id,req) ? Ok("Updated.") : NotFound($"Gate {id} not found.");
        }
        catch(InvalidOperationException ex){ return Conflict(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Failed to update gate: {ex.Message}"); }
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> Toggle(int id,[FromBody]bool isActive) {
        try {
            if(id<=0) return BadRequest("Invalid ID.");
            return await repo.ToggleActiveAsync(id,isActive) ? Ok(isActive?"Gate activated.":"Gate deactivated.") : NotFound($"Gate {id} not found.");
        }
        catch(Exception ex){ return StatusCode(500,$"Failed to update gate status: {ex.Message}"); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id) {
        try {
            if(id<=0) return BadRequest("Invalid ID.");
            return await repo.DeleteAsync(id) ? Ok("Deleted.") : NotFound($"Gate {id} not found.");
        }
        catch(InvalidOperationException ex){ return Conflict(ex.Message); }
        catch(Exception ex){ return StatusCode(500,$"Failed to delete gate: {ex.Message}"); }
    }
}
