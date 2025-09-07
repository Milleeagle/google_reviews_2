using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using google_reviews.Data;
using google_reviews.Models;
using google_reviews.Services;

namespace google_reviews.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ScheduledMonitorController : Controller
    {
        private readonly IScheduledMonitorService _scheduledMonitorService;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ScheduledMonitorController> _logger;

        public ScheduledMonitorController(
            IScheduledMonitorService scheduledMonitorService,
            IEmailService emailService,
            ApplicationDbContext context,
            ILogger<ScheduledMonitorController> logger)
        {
            _scheduledMonitorService = scheduledMonitorService;
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        // GET: ScheduledMonitor
        public async Task<IActionResult> Index()
        {
            var monitors = await _scheduledMonitorService.GetAllMonitorsAsync();
            ViewBag.EmailConfigured = await _emailService.IsConfiguredAsync();
            return View(monitors);
        }

        // GET: ScheduledMonitor/Create
        public async Task<IActionResult> Create()
        {
            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Companies = companies;
            ViewBag.EmailConfigured = await _emailService.IsConfiguredAsync();

            var model = new ScheduledReviewMonitor
            {
                ScheduleTime = new TimeSpan(9, 0, 0), // Default 9 AM
                MaxRating = 3,
                ReviewPeriodDays = 7,
                IncludeAllCompanies = true
            };

            return View(model);
        }

        // POST: ScheduledMonitor/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScheduledReviewMonitor monitor, List<string> selectedCompanies)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var createdMonitor = await _scheduledMonitorService.CreateMonitorAsync(monitor, selectedCompanies);
                    
                    _logger.LogInformation($"Created scheduled monitor: {monitor.Name}");
                    TempData["Success"] = $"Scheduled monitor '{monitor.Name}' created successfully! Next run: {createdMonitor.NextRunAt:MMM dd, yyyy HH:mm}";
                    
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scheduled monitor");
                ModelState.AddModelError("", "An error occurred while creating the scheduled monitor.");
            }

            // Reload companies for the view
            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Companies = companies;
            ViewBag.EmailConfigured = await _emailService.IsConfiguredAsync();
            return View(monitor);
        }

        // GET: ScheduledMonitor/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var monitor = await _scheduledMonitorService.GetMonitorAsync(id);
            if (monitor == null)
                return NotFound();

            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Companies = companies;
            ViewBag.SelectedCompanies = monitor.Companies.Select(c => c.CompanyId).ToList();
            ViewBag.EmailConfigured = await _emailService.IsConfiguredAsync();

            return View(monitor);
        }

        // POST: ScheduledMonitor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ScheduledReviewMonitor monitor, List<string> selectedCompanies)
        {
            if (id != monitor.Id)
                return NotFound();

            try
            {
                if (ModelState.IsValid)
                {
                    var updatedMonitor = await _scheduledMonitorService.UpdateMonitorAsync(id, monitor, selectedCompanies);
                    if (updatedMonitor == null)
                        return NotFound();

                    _logger.LogInformation($"Updated scheduled monitor: {monitor.Name}");
                    TempData["Success"] = $"Scheduled monitor '{monitor.Name}' updated successfully! Next run: {updatedMonitor.NextRunAt:MMM dd, yyyy HH:mm}";
                    
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating scheduled monitor");
                ModelState.AddModelError("", "An error occurred while updating the scheduled monitor.");
            }

            // Reload companies for the view
            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Companies = companies;
            ViewBag.SelectedCompanies = selectedCompanies;
            ViewBag.EmailConfigured = await _emailService.IsConfiguredAsync();
            return View(monitor);
        }

        // POST: ScheduledMonitor/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var success = await _scheduledMonitorService.DeleteMonitorAsync(id);
                if (success)
                {
                    _logger.LogInformation($"Deleted scheduled monitor: {id}");
                    TempData["Success"] = "Scheduled monitor deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Scheduled monitor not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting scheduled monitor: {id}");
                TempData["Error"] = "An error occurred while deleting the scheduled monitor.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: ScheduledMonitor/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            try
            {
                var success = await _scheduledMonitorService.ToggleMonitorActiveAsync(id);
                if (success)
                {
                    TempData["Success"] = "Monitor status updated successfully.";
                }
                else
                {
                    TempData["Error"] = "Monitor not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling monitor active status: {id}");
                TempData["Error"] = "An error occurred while updating the monitor status.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: ScheduledMonitor/History/5
        public async Task<IActionResult> History(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var monitor = await _scheduledMonitorService.GetMonitorAsync(id);
            if (monitor == null)
                return NotFound();

            var executions = await _scheduledMonitorService.GetExecutionHistoryAsync(id);
            
            ViewBag.Monitor = monitor;
            return View(executions);
        }

        // POST: ScheduledMonitor/TestEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TestEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Email address is required." });
            }

            try
            {
                var success = await _emailService.SendTestEmailAsync(email);
                if (success)
                {
                    return Json(new { success = true, message = "Test email sent successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to send test email. Check email configuration." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending test email to {email}");
                return Json(new { success = false, message = "An error occurred while sending test email." });
            }
        }

        // POST: ScheduledMonitor/RunNow/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunNow(string id)
        {
            try
            {
                // This is a simplified version - in practice you might want to queue this
                await _scheduledMonitorService.ProcessScheduledMonitorsAsync();
                TempData["Success"] = "Scheduled monitors have been processed. Check execution history for results.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running scheduled monitors now");
                TempData["Error"] = "An error occurred while processing scheduled monitors.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}