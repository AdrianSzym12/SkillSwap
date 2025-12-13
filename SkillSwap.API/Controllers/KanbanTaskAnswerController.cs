using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;

namespace SkillSwap.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class KanbanTaskAnswerController : ControllerBase
    {
        private readonly IKanbanTaskAnswerService _answerService;

        public KanbanTaskAnswerController(IKanbanTaskAnswerService answerService)
        {
            _answerService = answerService;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == ClaimTypes.NameIdentifier);

            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId))
                return null;

            return userId;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var result = await _answerService.GetAsync(id);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);
            return Ok(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _answerService.GetAsync();
            if (!result.IsSuccess)
                return Problem(detail: result.Message);
            return Ok(result.Data);
        }

        [HttpGet("task/{taskId:int}")]
        public async Task<IActionResult> GetByTask(int taskId, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _answerService.GetByTaskAsync(taskId, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] KanbanTaskAnswerDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _answerService.AddAsync(request, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return CreatedAtAction(nameof(Get), new { id = result.Data.Id }, result.Data);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] KanbanTaskAnswerDTO request, CancellationToken ct)
        {
            if (id != request.Id)
                return BadRequest("Id w ścieżce różni się od Id w treści żądania.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _answerService.UpdateAsync(request, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _answerService.DeleteAsync(id, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(new { message = result.Message });
        }
    }
}
