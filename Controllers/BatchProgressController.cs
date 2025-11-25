using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using google_reviews.Services;

namespace google_reviews.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BatchProgressController : ControllerBase
    {
        private readonly BatchProgressService _progressService;

        public BatchProgressController(BatchProgressService progressService)
        {
            _progressService = progressService;
        }

        [HttpGet("{sessionId}")]
        public ActionResult<BatchProgressInfo> GetProgress(string sessionId)
        {
            var progress = _progressService.GetProgress(sessionId);
            if (progress == null)
            {
                return NotFound();
            }

            return Ok(progress);
        }
    }
}