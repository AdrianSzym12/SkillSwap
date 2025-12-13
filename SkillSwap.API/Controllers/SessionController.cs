using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;

namespace SkillSwap.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var result = await _sessionService.LoginAsync(request);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data); 
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader))
                return BadRequest("Missing Authorization header");

            var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring("Bearer ".Length).Trim()
                : authHeader.Trim();

            var result = await _sessionService.LogoutAsync(token);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(new { message = result.Message });
        }
        [HttpPost("current")]
        public async Task<IActionResult> Current(
        [FromHeader(Name = "Authentication")] string authentication,
        CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(authentication))
                return Unauthorized("Missing Authentication header");

            var token = authentication.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authentication.Substring("Bearer ".Length).Trim()
                : authentication.Trim();

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized("Empty token");

            var result = await _sessionService.GetCurrentAsync(token);
            if (!result.IsSuccess)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, result.Message);
            }

            return Ok(result.Data);
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            // ApiController sam waliduje DataAnnotations i zwróci 400 jeśli coś nie gra

            try
            {
                var response = await _sessionService.RegisterAsync(dto);

                // zawsze 201 - bo User + Profile zostały utworzone
                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

    }
}
