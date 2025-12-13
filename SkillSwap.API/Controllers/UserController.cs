using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Entities.Database;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SkillSwap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{id:int}")]
        
        public async Task<ActionResult<UserDTO>> GetAsync(int id, CancellationToken ct)
        {
            var result = await _userService.GetAsync(id);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);
            return Ok(result?.Data);
        }

        [HttpPost]
        
        public async Task<IActionResult> Create([FromBody] UserDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _userService.AddAsync(request);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Created();
        }
        [HttpPut("{id:int}")]
        
        public async Task<IActionResult> Update(int id, [FromBody] UserDTO request, CancellationToken ct)
        {
            if (id != request.Id)
                return BadRequest("The Id in the path is different from the Id in the request body.");

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _userService.UpdateAsync(request);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result?.Data);
        }
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId is null)
                return Unauthorized("Invalid token");

            var result = await _userService.DeleteAsync(id, currentUserId.Value);
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
