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
    public class UserSkillController : ControllerBase
    {
        private readonly IUserSkillService _userSkillService;

        public UserSkillController(IUserSkillService userSkillService)
        {
            _userSkillService = userSkillService;
        }

       private int? GetCurrentUserId()
        {
            var value =
                User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("nameid");

            return int.TryParse(value, out var userId) ? userId : null;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _userSkillService.GetMeAsync(userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync(CancellationToken ct)
        {
            var result = await _userSkillService.GetAsync();
            if (!result.IsSuccess)
                return Problem(detail: result.Message);
            return Ok(result.Data);
        }

        [HttpPost("me")]
        public async Task<IActionResult> CreateMe([FromBody] UserSkillCreateMeDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _userSkillService.AddMeAsync(request, userId.Value);
            if (!result.IsSuccess)
            {
                if (result.Message == "UserSkill already exists")
                    return Conflict(new { message = result.Message });

                return BadRequest(new { message = result.Message });
            }

            return CreatedAtRoute("GetUserSkillById", new { id = result.Data.Id }, result.Data);
        }
        [HttpPut("me/{id:int}")]
        public async Task<IActionResult> UpdateMe(int id, [FromBody] UserSkillUpdateMeDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized(new { message = "Invalid user token" });

            var result = await _userSkillService.UpdateMeAsync(id, request, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }


        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteMe(int id, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _userSkillService.DeleteAsync(id, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(new { message = result.Message });
        }
    }
}
