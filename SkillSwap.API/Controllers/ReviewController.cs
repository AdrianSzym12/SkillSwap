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
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        private int? GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("nameid");

            return int.TryParse(value, out var userId) ? userId : null;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var result = await _reviewService.GetAsync(id);
            if (!result.IsSuccess)
                return NotFound(new { message = result.Message });

            return Ok(result.Data);
        }

        [HttpGet("profile/{profileId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByProfile(int profileId, CancellationToken ct)
        {
            var result = await _reviewService.GetByProfileAsync(profileId);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        [HttpGet("match/{matchId:int}")]
        public async Task<IActionResult> GetByMatch(int matchId, CancellationToken ct)
        {
            var result = await _reviewService.GetByMatchAsync(matchId);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        [HttpGet("me/given")]
        public async Task<IActionResult> GetMyGiven(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token" });

            var result = await _reviewService.GetMyGivenAsync(userId.Value);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        [HttpGet("me/received")]
        public async Task<IActionResult> GetMyReceived(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token" });

            var result = await _reviewService.GetMyReceivedAsync(userId.Value);
            if (!result.IsSuccess)
                return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ReviewCreateDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token" });

            var result = await _reviewService.AddAsync(request, userId.Value);

            if (!result.IsSuccess)
            {
                if (result.Message == "Match not found")
                    return NotFound(new { message = result.Message });

                if (result.Message == "You have already reviewed this match")
                    return Conflict(new { message = result.Message });

                if (result.Message == "You are not a participant of this match")
                    return StatusCode(403, new { message = result.Message });

                return BadRequest(new { message = result.Message });
            }

            return StatusCode(201, result.Data);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid token" });

            var result = await _reviewService.DeleteAsync(id, userId.Value);

            if (!result.IsSuccess)
            {
                if (result.Message == "Review not found")
                    return NotFound(new { message = result.Message });

                if (result.Message == "You are not allowed to delete this review")
                    return StatusCode(403, new { message = result.Message });

                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}
