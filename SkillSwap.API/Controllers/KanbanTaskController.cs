using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Database;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class KanbanTaskController : ControllerBase
{
    private readonly IKanbanTaskService _taskService;
    public KanbanTaskController(IKanbanTaskService taskService) => _taskService = taskService;

    private int? GetCurrentUserId()
    {
        var idClaim = User.Claims.FirstOrDefault(c =>
            c.Type == JwtRegisteredClaimNames.Sub ||
            c.Type == ClaimTypes.NameIdentifier);

        return (idClaim != null && int.TryParse(idClaim.Value, out var userId)) ? userId : null;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var result = await _taskService.GetAsync(id, ct);
        if (!result.IsSuccess) return Problem(detail: result.Message);
        return Ok(result.Data);
    }

    [HttpGet("board/{boardId:int}")]
    public async Task<IActionResult> GetByBoard(int boardId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized("Invalid token");

        var result = await _taskService.GetByBoardAsync(boardId, userId.Value, ct);
        if (!result.IsSuccess) return Problem(detail: result.Message);

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KanbanTaskCreateDTO request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized("Invalid token");

        var result = await _taskService.AddAsync(request, userId.Value, ct);
        if (!result.IsSuccess) return Problem(detail: result.Message);

        return CreatedAtAction(nameof(Get), new { id = result.Data.Id }, result.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] KanbanTaskUpdateDTO request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized("Invalid token");

        var result = await _taskService.UpdateAsync(id, request, userId.Value, ct);
        if (!result.IsSuccess) return Problem(detail: result.Message);

        return Ok(result.Data);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized("Invalid token");

        var result = await _taskService.DeleteAsync(id, userId.Value, ct);
        if (!result.IsSuccess) return Problem(detail: result.Message);

        return Ok(new { message = result.Message });
    }
}
