using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly StudentPartTimeJobDbContext _context;

        public HomeController(ILogger<HomeController> logger, StudentPartTimeJobDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var query = _context.Jobs
                .Include(j => j.Employer).ThenInclude(e => e.Company)
                .Include(j => j.Province)
                .Include(j => j.JobType)
                .Where(j => j.Status == "Approved" && j.Deadline >= DateOnly.FromDateTime(DateTime.Today));

            if (User.IsInRole("Student"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdStr, out int userId))
                {
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (student != null)
                    {
                        var appliedJobIds = await _context.Applications
                            .Where(a => a.StudentId == student.StudentId && a.Status != "Rejected")
                            .Select(a => a.JobId)
                            .ToListAsync();

                        query = query.Where(j => !appliedJobIds.Contains(j.JobId));
                    }
                }
            }

            var latestJobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(latestJobs);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
