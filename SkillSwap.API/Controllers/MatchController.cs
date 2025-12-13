using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces.ExternalInterfaces;

namespace SkillSwap.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MatchController : ControllerBase
    {
        private readonly IMatchService _matchService;
        private readonly IMatchSuggestion _matchSuggestionService;

        public MatchController(IMatchService matchService, IMatchSuggestion matchSuggestionService)
        {
            _matchService = matchService;
            _matchSuggestionService = matchSuggestionService;
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
            var result = await _matchService.GetAsync(id);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _matchService.GetAsync();
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMy(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchService.GetMyAsync(userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MatchDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _matchService.AddAsync(request);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return CreatedAtAction(nameof(Get), new { id = result.Data.Id }, result.Data);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] MatchDTO request, CancellationToken ct)
        {
            if (id != request.Id)
                return BadRequest("Id w ścieżce różni się od Id w treści żądania.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchService.UpdateAsync(request, userId.Value);
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

            var result = await _matchService.DeleteAsync(id, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(new { message = result.Message });
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] int limit = 20, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchSuggestionService.GetSuggestionsAsync(userId.Value, limit);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [HttpPost("{profileId:int}/like")]
        public async Task<IActionResult> Like(int profileId, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchService.LikeAsync(profileId, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [HttpPost("{profileId:int}/dislike")]
        public async Task<IActionResult> Dislike(int profileId, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchService.DislikeAsync(profileId, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(new { message = result.Message });
        }
    }
}
