using Microsoft.AspNetCore.Mvc;
using SkillSwap.Domain.Entities.Commons;

namespace SkillSwap.API.Helpers
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result) where T : class
        {
            if (result is null)
                return controller.Problem(statusCode: 500, detail: "Result was null");

            if (result.IsSuccess)
                return controller.Ok(result.Data);

            return controller.ProblemFromResult(result);
        }

        public static IActionResult ProblemFromResult<T>(this ControllerBase controller, Result<T> result) where T : class
        {
            // In clean HTTP mapping the status code must come from the application layer.
            // If not provided, fall back to 400 (Bad Request) rather than guessing from message text.
            var status = result.StatusCode ?? 400;

            return controller.Problem(
                statusCode: status,
                detail: result.Message ?? "Request failed");
        }
    }
}
