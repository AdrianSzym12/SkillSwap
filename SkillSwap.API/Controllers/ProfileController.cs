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
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var result = await _profileService.GetAsync(id);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _profileService.GetByUserIdAsync(userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProfileDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _profileService.AddAsync(request, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return CreatedAtAction(nameof(Get), new { id = result.Data.id }, result.Data);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProfileDTO request, CancellationToken ct)
        {
            if (id != request.id)
                return BadRequest("Id w ścieżce różni się od Id w treści żądania.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _profileService.UpdateAsync(request, userId.Value);
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

            var result = await _profileService.DeleteAsync(id, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(new { message = result.Message });
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
    }
}
