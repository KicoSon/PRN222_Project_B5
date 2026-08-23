using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StudentPartTime.Services;

namespace StudentPartTime.ViewComponents;

/// <summary>
/// FEATURE: ONLINE-CV
/// Renders "Việc làm phù hợp với kỹ năng của bạn" for a logged-in Student.
/// Any other role (or anonymous) receives an empty result, so the section
/// simply does not render.
/// </summary>
public class RecommendedJobsViewComponent : ViewComponent
{
    private readonly IJobRecommendationService _recommendationService;

    public RecommendedJobsViewComponent(IJobRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (HttpContext.User?.Identity?.IsAuthenticated != true ||
            !HttpContext.User.IsInRole("Student"))
        {
            return Content(string.Empty);
        }

        var userIdStr = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
            return Content(string.Empty);

        var items = await _recommendationService.GetRecommendedJobsAsync(userId, 6);
        return View(items);
    }
}