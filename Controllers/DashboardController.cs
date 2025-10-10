using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "DashboardAccess")]  // Protects with token and permission check
public class DashboardController : ControllerBase
{
    [HttpGet]
    public IActionResult GetDashboard([FromQuery] string monthRange)
    {
        if (string.IsNullOrEmpty(monthRange))
            return BadRequest("monthRange is required");

        // Simulate data based on monthRange (customize with real DB/logic)
        var data = new
        {
            MonthRange = monthRange,
            Metrics = new
            {
                TotalTransactions = monthRange == "last3Months" ? 1500 : 500,
                Revenue = monthRange == "last3Months" ? 25000.00 : 8000.00,
                UsersActive = monthRange == "last3Months" ? 120 : 40
            },
            Timestamp = DateTime.UtcNow  // Current date: October 10, 2025
        };

        return Ok(data);
    }
}