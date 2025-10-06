using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.Service;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerLogController : Controller
    {
        private readonly ServerLogService _serverLogService;

        public ServerLogController(ServerLogService serverLogService)
        {
            _serverLogService = serverLogService;
        }

        /// <summary>
        /// Gets all server logs currently in memory.
        /// </summary>
        /// <returns>Server logs as plain text</returns>
        [HttpGet]
        public string GetLogs()
        {
            return _serverLogService.GetAllLogs();
        }

        /// <summary>
        /// Clears all server logs from memory.
        /// </summary>
        [HttpDelete]
        public void ClearLogs()
        {
            _serverLogService.Clear();
        }
    }
}
