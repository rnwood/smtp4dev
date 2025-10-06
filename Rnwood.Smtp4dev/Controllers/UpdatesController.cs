using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.ApiModel;
using Rnwood.Smtp4dev.Service;

namespace Rnwood.Smtp4dev.Controllers
{
    /// <summary>
    /// Handles update notifications and what's new functionality
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UpdatesController : Controller
    {
        private readonly UpdateNotificationService _updateService;

        public UpdatesController(UpdateNotificationService updateService)
        {
            _updateService = updateService;
        }

        /// <summary>
        /// Checks for updates and new releases
        /// </summary>
        /// <param name="username">Optional username to track version per user</param>
        /// <returns>Update check result with available updates and what's new information</returns>
        [HttpGet("check")]
        public async Task<ActionResult<UpdateCheckResult>> CheckForUpdates([FromQuery] string username = null)
        {
            var result = await _updateService.CheckForUpdatesAsync(username);
            return Ok(result);
        }

        /// <summary>
        /// Marks a version as seen by the user
        /// </summary>
        /// <param name="username">Username to track version for</param>
        /// <param name="version">Version that was seen</param>
        [HttpPost("mark-seen")]
        public async Task<ActionResult> MarkVersionAsSeen([FromQuery] string username, [FromQuery] string version)
        {
            await _updateService.MarkVersionAsSeenAsync(username ?? "anonymous", version);
            return Ok();
        }
    }
}
