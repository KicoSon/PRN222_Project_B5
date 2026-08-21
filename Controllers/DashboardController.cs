using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPartTime.Services;

namespace StudentPartTime.Controllers;

[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> JobStatistics()
    {
        var model = await _dashboardService.GetJobStatisticsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ApplicationStatistics()
    {
        var model = await _dashboardService.GetApplicationStatisticsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CompanyStatistics()
    {
        var model = await _dashboardService.GetCompanyStatisticsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> StudentStatistics()
    {
        var model = await _dashboardService.GetStudentStatisticsAsync();
        return View(model);
    }
}
