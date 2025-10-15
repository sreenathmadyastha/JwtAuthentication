using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CategorySummarizer;
/// <summary>
/// Web API Controller to generate the category summary JSON.
/// Assumes ASP.NET Core Web API setup. Injects a service for database rows (e.g., via repository).
/// For demonstration, uses hardcoded rows; in production, fetch from DB.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategorySummaryController : ControllerBase
{
    private readonly CategorySummary _summaryCalculator;

    public CategorySummaryController()
    {
        _summaryCalculator = new CategorySummary();
    }

    /// <summary>
    /// GET endpoint to compute and return the category summary JSON.
    /// </summary>
    /// <param name="monthRange">Month range for reference (default: 6).</param>
    /// <returns>The root summary as JSON.</returns>
    [HttpGet]
    public IActionResult GetSummary([FromQuery] int monthRange = 6)
    {
        var rows = new List<Row>
            {
                // Money In sample rows
                new() { MonthYear = "Jul 25", Category = "Processed", SubCategory = "Processed", Amount = 1101m, Type = "In" },
                new() { MonthYear = "Jul 25", Category = "upcoming payments", SubCategory = "In process", Amount = 1101m, Type = "In" },
                new() { MonthYear = "Jul 25", Category = "upcoming payments", SubCategory = "scheduled", Amount = 2100m, Type = "In" },
                new() { MonthYear = "Jul 25", Category = "upcoming payments", SubCategory = "inProcess", Amount = 1110m, Type = "In" },
                new() { MonthYear = "Aug 25", Category = "Processed", SubCategory = "Processed", Amount = 1101m, Type = "In" },
                new() { MonthYear = "Sep 25", Category = "Processed", SubCategory = "Processed", Amount = 1101m, Type = "In" },

                // Money Out sample rows
                new() { MonthYear = "Jul 25", Category = "paid", SubCategory = "paid", Amount = 5000m, Type = "Out" },
                new() { MonthYear = "Jul 25", Category = "inprocess", SubCategory = "inprocess", Amount = 2000m, Type = "Out" },
                new() { MonthYear = "Aug 25", Category = "open", SubCategory = "sent", Amount = 3000m, Type = "Out" },
                new() { MonthYear = "Aug 25", Category = "open", SubCategory = "overDue", Amount = 1000m, Type = "Out" },
                new() { MonthYear = "Sep 25", Category = "paid", SubCategory = "paid", Amount = 1500m, Type = "Out" }
                // Add more sample data to match desired amounts if needed
            };

        var asOfDate = new DateTime(2025, 10, 14); // Current date
        var rootObject = _summaryCalculator.ComputeSummary(rows, monthRange, asOfDate);

        // ASP.NET Core automatically serializes to JSON using System.Text.Json
        return Ok(rootObject);
    }

    /// <summary>
    /// POST endpoint to compute summary from provided rows.
    /// </summary>
    /// <param name="request">Request with rows and month range.</param>
    /// <returns>The root summary as JSON.</returns>
    [HttpPost]
    public IActionResult PostSummary([FromBody] SummaryRequest request)
    {
        if (request == null || request.Rows == null)
            return BadRequest("Invalid request.");

        var asOfDate = new DateTime(2025, 10, 14); // Or from request if provided
        var rootSummary = _summaryCalculator.ComputeSummary(request.Rows, request.MonthRange, asOfDate);

        return Ok(rootSummary);
    }
}

/// <summary>
/// Request model for POST endpoint.
/// </summary>
public class SummaryRequest
{
    public int MonthRange { get; set; } = 6;
    public List<Row> Rows { get; set; } = new();
}
