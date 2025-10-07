using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.Service;
using System.Collections.Generic;
using System.Linq;

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
        /// Gets all server logs currently in memory (formatted as text).
        /// </summary>
        /// <returns>Server logs as plain text</returns>
        [HttpGet]
        public string GetLogs()
        {
            return _serverLogService.GetAllLogs();
        }

        /// <summary>
        /// Gets structured log entries with optional filtering.
        /// </summary>
        /// <param name="level">Optional log level filter (e.g., "Information", "Warning", "Error")</param>
        /// <param name="source">Optional source filter</param>
        /// <param name="search">Optional text search filter</param>
        /// <returns>Array of structured log entries</returns>
        [HttpGet("entries")]
        public IEnumerable<LogEntry> GetLogEntries(string level = null, string source = null, string search = null)
        {
            var entries = _serverLogService.GetAllLogEntries();

            // Apply filters
            if (!string.IsNullOrEmpty(level))
            {
                entries = entries.Where(e => e.Level.Equals(level, System.StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(source))
            {
                entries = entries.Where(e => e.Source.Equals(source, System.StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(search))
            {
                entries = entries.Where(e => 
                    e.Message.Contains(search, System.StringComparison.OrdinalIgnoreCase) ||
                    e.Exception.Contains(search, System.StringComparison.OrdinalIgnoreCase));
            }

            return entries;
        }

        /// <summary>
        /// Gets distinct log sources currently in the log buffer.
        /// </summary>
        /// <returns>Array of unique source names</returns>
        [HttpGet("sources")]
        public IEnumerable<string> GetLogSources()
        {
            return _serverLogService.GetAllLogEntries()
                .Select(e => e.Source)
                .Distinct()
                .OrderBy(s => s);
        }

        /// <summary>
        /// Gets distinct log levels currently in the log buffer.
        /// </summary>
        /// <returns>Array of unique log levels</returns>
        [HttpGet("levels")]
        public IEnumerable<string> GetLogLevels()
        {
            return _serverLogService.GetAllLogEntries()
                .Select(e => e.Level)
                .Distinct()
                .OrderBy(l => l);
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
