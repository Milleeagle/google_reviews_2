using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using google_reviews.Data;

namespace google_reviews.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize]
    public class ApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/companies
        [HttpGet("companies")]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var companies = await _context.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name, c.PlaceId })
                    .ToListAsync();

                return Ok(companies);
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving companies." });
            }
        }
    }
}