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

        [HttpGet("{id:int}", Name = "GetUserSkillById")]
        public async Task<IActionResult> GetAsync(int id, CancellationToken ct)
        {
            var result = await _userSkillService.GetAsync(id);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);
            return Ok(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync(CancellationToken ct)
        {
            var result = await _userSkillService.GetAsync();
            if (!result.IsSuccess)
                return Problem(detail: result.Message);
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserSkillDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _userSkillService.AddAsync(request, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return CreatedAtRoute("GetUserSkillById", new { id = result.Data.Id }, result.Data);
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
                return Problem(detail: result.Message);

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


        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserSkillDTO request, CancellationToken ct)
        {
            if (id != request.Id)
                return BadRequest("The Id in the path is different from the Id in the request body.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized("Invalid token");

            var result = await _userSkillService.UpdateAsync(request, userId.Value);
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

            var result = await _userSkillService.DeleteAsync(id, userId.Value);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(new { message = result.Message });
        }
    }
}
