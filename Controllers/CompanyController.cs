using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPartTime.Models;
using StudentPartTime.Services;

namespace StudentPartTime.Controllers;

[Authorize(Roles = "Admin")]
public class CompanyController : Controller
{
    private const int PageSize = 10;

    private readonly ICompanyService _companyService;
    private readonly IAuditService _auditService;

    public CompanyController(ICompanyService companyService, IAuditService auditService)
    {
        _companyService = companyService;
        _auditService = auditService;
    }

    private int? GetCurrentAdminId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idString, out int id) ? id : (int?)null;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var (items, totalCount) = await _companyService.GetPagedAsync(search, status, page, PageSize);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        ViewBag.TotalCount = totalCount;

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
            return NotFound();

        var model = new CompanyDetailViewModel
        {
            CompanyId = company.CompanyId,
            CompanyName = company.CompanyName,
            LogoUrl = company.LogoUrl,
            Website = company.Website,
            Email = company.Email,
            Phone = company.Phone,
            Description = company.Description,
            Address = company.Address,
            ProvinceName = company.Province?.ProvinceName,
            Status = company.Status,
            CreatedAt = company.CreatedAt,
            EmployerCount = company.Employers.Count
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var (success, errorMessage) = await _companyService.ApproveAsync(id);
        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }

        await _auditService.LogActionAsync(GetCurrentAdminId(), "Approve Company", "Company", id, "Admin approved company");

        TempData["SuccessMessage"] = "Đã duyệt công ty thành công.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var (success, errorMessage) = await _companyService.RejectAsync(id, reason);
        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }

        await _auditService.LogActionAsync(GetCurrentAdminId(), "Reject Company", "Company", id, $"Admin rejected company. Reason: {reason}");

        TempData["SuccessMessage"] = "Đã từ chối công ty.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(int id)
    {
        var (success, errorMessage) = await _companyService.BlockAsync(id);
        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }

        await _auditService.LogActionAsync(GetCurrentAdminId(), "Block Company", "Company", id, "Admin blocked company");

        TempData["SuccessMessage"] = "Đã chặn công ty.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var (success, errorMessage) = await _companyService.ActivateAsync(id);
        if (!success)
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }

        await _auditService.LogActionAsync(GetCurrentAdminId(), "Activate Company", "Company", id, "Admin re-activated company");

        TempData["SuccessMessage"] = "Đã kích hoạt lại công ty.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
