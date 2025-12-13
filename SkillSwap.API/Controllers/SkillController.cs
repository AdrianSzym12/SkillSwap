using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillSwap.Application.DTO;
using SkillSwap.Application.Interfaces;
using SkillSwap.Domain.Enums;

namespace SkillSwap.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SkillController : ControllerBase
    {
        private readonly ISkillService _skillService;
        public SkillController(ISkillService skillService)
        {
            _skillService = skillService;
        }
        [AllowAnonymous]
        [HttpGet("{id:int}", Name = "GetSkillById")]
        public async Task<IActionResult> GetAsync(int id, CancellationToken ct)
        {
            var result = await _skillService.GetAsync(id);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);
            return Ok(result.Data);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GeAsync(CancellationToken ct)
        {
            var result = await _skillService.GetAsync();
            if (!result.IsSuccess)
                return Problem(detail: result.Message);
            return Ok(result.Data);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] SkillCategory? category, CancellationToken ct)
        {
            var result = await _skillService.SearchAsync(q, category);
            if (!result.IsSuccess)
                return Problem(detail: result.Message);

            return Ok(result.Data);
        }

    }
}
