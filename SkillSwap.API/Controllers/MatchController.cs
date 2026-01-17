using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.API.Helpers;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Application.Interfaces.ExternalInterfaces;
using SkillSwap.Domain.Entities.Commons;
using SkillSwap.Infrastructure.MatchLogic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

        // ===== Pomocnicze: userId z JWT =====
        private int? GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == ClaimTypes.NameIdentifier);

            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId))
                return null;

            return userId;
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var result = await _matchService.GetAsync(id, ct);
            if (!result.IsSuccess)
                return this.ProblemFromResult(result);

            return Ok(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var result = await _matchService.GetAsync(ct);
            if (!result.IsSuccess)
                return this.ProblemFromResult(result);

            return Ok(result.Data);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMy(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchService.GetMyAsync(userId.Value, ct);
            if (!result.IsSuccess)
                return this.ProblemFromResult(result);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] MatchDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _matchService.AddAsync(request, ct);
            if (!result.IsSuccess)
                return this.ProblemFromResult(result);

            return CreatedAtAction(nameof(Get), new { id = result.Data.Id }, result.Data);
        }

        [HttpPut("{id:int:min(1)}")]
        public async Task<IActionResult> Update(int id, [FromBody] MatchDTO request, CancellationToken ct)
        {
            if (id != request.Id)
                return BadRequest("Id w ścieżce różni się od Id w treści żądania.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchService.UpdateAsync(request, userId.Value, ct);
            if (!result.IsSuccess)
                return this.ProblemFromResult(result);

            return Ok(result.Data);
        }

        [HttpDelete("{id:int:min(1)}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchService.DeleteAsync(id, userId.Value, ct);
            if (!result.IsSuccess)
                return this.ProblemFromResult(result);

            return Ok(new { message = result.Message });
        }

        // ===== Sugestie matchy (algorytm dopasowania) =====
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery, Range(1, 50)] int limit = 20, CancellationToken ct = default)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchSuggestionService.GetSuggestionsAsync(userId.Value, limit, ct);
            if (!result.IsSuccess)
                return this.ProblemFromResult(result);

            return Ok(result.Data);
        }

        // ===== Swipe LIKE =====
        [HttpPost("{profileId:int:min(1)}/like")]
        public async Task<IActionResult> Like(int profileId, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchService.LikeAsync(profileId, userId.Value, ct);
            if (!result.IsSuccess)
                return this.ProblemFromResult(result);

            return Ok(result.Data);
        }

        // ===== Swipe DISLIKE =====
        [HttpPost("{profileId:int:min(1)}/dislike")]
        public async Task<IActionResult> Dislike(int profileId, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _matchService.DislikeAsync(profileId, userId.Value, ct);
            if (!result.IsSuccess)
                return this.ProblemFromResult(result);

            return Ok(result.Data);
        }
    }
}
