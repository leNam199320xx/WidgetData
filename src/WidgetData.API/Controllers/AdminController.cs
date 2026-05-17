using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WidgetData.Infrastructure.Tools;

namespace WidgetData.API.Controllers;

/// <summary>
/// Admin-only endpoints for system maintenance and migration
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly DbToJsonMigrationTool _migrationTool;
    private readonly ILogger<AdminController> _logger;

    public AdminController(DbToJsonMigrationTool migrationTool, ILogger<AdminController> logger)
    {
        _migrationTool = migrationTool;
        _logger = logger;
    }

    /// <summary>
    /// Migrate all data from database to JSON files
    /// WARNING: This is a one-time operation. Use with caution.
    /// Only accessible to SuperAdmin role.
    /// </summary>
    [HttpPost("migrate-db-to-json")]
    public async Task<IActionResult> MigrateDbToJson()
    {
        try
        {
            _logger.LogWarning("SuperAdmin initiated database to JSON migration");
            await _migrationTool.MigrateAllAsync();
            return Ok(new { message = "Migration completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
