using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace CategorySummarizer
{
    /// <summary>
    /// Represents the root object with insight summary (monthly) and money in/out summaries (overall).
    /// </summary>
    public class InsightsRoot
    {
        /// <summary>
        /// List of monthly insights with totals.
        /// </summary>
        [JsonPropertyName("insightSummary")]
        public List<MonthlyInsight> InsightSummary { get; set; } = new();

        /// <summary>
        /// The overall money in summary across all months.
        /// </summary>
        [JsonPropertyName("moneyInSummary")]
        public MoneySummary MoneyInSummary { get; set; } = new();

        /// <summary>
        /// The overall money out summary across all months.
        /// </summary>
        [JsonPropertyName("moneyOutSummary")]
        public MoneySummary MoneyOutSummary { get; set; } = new();
    }

    /// <summary>
    /// Represents a monthly insight entry.
    /// </summary>
    public class MonthlyInsight
    {
        /// <summary>
        /// The index of the month (1-based, sorted chronologically).
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// The month string (e.g., "Jul 25").
        /// </summary>
        [JsonPropertyName("month")]
        public string Month { get; set; } = string.Empty;

        /// <summary>
        /// The summary for this month.
        /// </summary>
        [JsonPropertyName("summary")]
        public MonthlySummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Represents the monthly summary with in and out totals.
    /// </summary>
    public class MonthlySummary
    {
        /// <summary>
        /// The total money in for this month.
        /// </summary>
        [JsonPropertyName("moneyInTotal")]
        public decimal MoneyInTotal { get; set; }

        /// <summary>
        /// The total money out for this month.
        /// </summary>
        [JsonPropertyName("moneyOutTotal")]
        public decimal MoneyOutTotal { get; set; }
    }

    /// <summary>
    /// Represents the overall money summary structure for JSON serialization (used for both in and out).
    /// </summary>
    public class MoneySummary
    {
        /// <summary>
        /// The month range as a string (e.g., "6").
        /// </summary>
        [JsonPropertyName("monthRange")]
        public string MonthRange { get; set; } = string.Empty;

        /// <summary>
        /// The overall summary with grand total.
        /// </summary>
        [JsonPropertyName("summary")]
        public OverallSummary Summary { get; set; } = new();

        /// <summary>
        /// Dictionary of category details, keyed by normalized (lowercase) category name.
        /// </summary>
        [JsonPropertyName("categories")]
        public Dictionary<string, CategoryDetail> Categories { get; set; } = new();
    }

    /// <summary>
    /// Represents the overall summary with grand total.
    /// </summary>
    public class OverallSummary
    {
        /// <summary>
        /// The grand total sum of all category totals.
        /// </summary>
        [JsonPropertyName("total")]
        public decimal Total { get; set; }
    }

    /// <summary>
    /// Represents a category detail with amount and optional subitems.
    /// </summary>
    public class CategoryDetail
    {
        /// <summary>
        /// The total amount for this category.
        /// </summary>
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Optional dictionary of subcategory totals, keyed by normalized (lowercase) subcategory name.
        /// </summary>
        [JsonPropertyName("subitem")]
        public Dictionary<string, decimal>? Subitem { get; set; }
    }

    /// <summary>
    /// Represents a database row input.
    /// </summary>
    public class Row
    {
        public string MonthYear { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = "In"; // "In" or "Out"
    }

    /// <summary>
    /// Computes the category summary from database rows.
    /// Categories and subcategories are normalized to lowercase for keys.
    /// MonthRange is for reference only and not used for filtering.
    /// </summary>
    public class CategorySummary
    {
        /// <summary>
        /// Computes the summary for the given rows.
        /// Processes all rows without date filtering.
        /// Generates monthly insights and overall money in/out summaries.
        /// </summary>
        /// <param name="rows">List of database rows.</param>
        /// <param name="monthRange">Month range for reference (e.g., 6).</param>
        /// <param name="asOfDate">As-of date for reference (unused).</param>
        /// <returns>The root object ready for JSON serialization.</returns>
        public InsightsRoot ComputeSummary(IEnumerable<Row> rows, int monthRange, DateTime asOfDate)
        {
            var root = new InsightsRoot
            {
                InsightSummary = new List<MonthlyInsight>(),
                MoneyInSummary = new MoneySummary
                {
                    MonthRange = monthRange.ToString(),
                    Summary = new OverallSummary(),
                    Categories = new Dictionary<string, CategoryDetail>()
                },
                MoneyOutSummary = new MoneySummary
                {
                    MonthRange = monthRange.ToString(),
                    Summary = new OverallSummary(),
                    Categories = new Dictionary<string, CategoryDetail>()
                }
            };

            if (rows == null || !rows.Any())
            {
                ComputeMonthlyTotals(rows, root);
                return root;
            }

            // Process all rows without date filtering, filter for non-empty cat/sub for categories
            var processedRows = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Category) && !string.IsNullOrWhiteSpace(r.SubCategory))
                .ToList();

            // Compute monthly insights
            ComputeMonthlyTotals(rows, root);

            // Separate in and out rows for category summaries
            var inRows = processedRows.Where(r => r.Type.ToLowerInvariant() == "in").ToList();
            var outRows = processedRows.Where(r => r.Type.ToLowerInvariant() == "out").ToList();

            // Compute money in summary
            root.MoneyInSummary = ComputeMoneySummary(inRows, monthRange);

            // Compute money out summary
            root.MoneyOutSummary = ComputeMoneySummary(outRows, monthRange);

            return root;
        }

        /// <summary>
        /// Computes the money summary structure for given rows.
        /// </summary>
        private MoneySummary ComputeMoneySummary(IEnumerable<Row> rows, int monthRange)
        {
            var moneySummary = new MoneySummary
            {
                MonthRange = monthRange.ToString(),
                Summary = new OverallSummary(),
                Categories = new Dictionary<string, CategoryDetail>()
            };

            if (!rows.Any()) return moneySummary;

            // Group by normalized category and subcategory, sum amounts
            var grouped = rows
                .GroupBy(r => new { CategoryKey = r.Category.ToLowerInvariant(), SubCategoryKey = r.SubCategory.ToLowerInvariant() })
                .Select(g => new { g.Key.CategoryKey, g.Key.SubCategoryKey, Sum = g.Sum(r => r.Amount) });

            // Aggregate subcategories per category
            var categoryGroups = grouped.GroupBy(g => g.CategoryKey);
            decimal grandTotal = 0m;

            foreach (var catGroup in categoryGroups)
            {
                var categoryKey = catGroup.Key;
                var subDict = catGroup.ToDictionary(g => g.SubCategoryKey, g => g.Sum);

                var categoryTotal = subDict.Values.Sum();
                grandTotal += categoryTotal;

                var detail = new CategoryDetail
                {
                    Amount = categoryTotal,
                    Subitem = subDict.Count > 0 ? subDict : null
                };

                moneySummary.Categories[categoryKey] = detail;
            }

            moneySummary.Summary.Total = grandTotal;
            return moneySummary;
        }

        /// <summary>
        /// Computes monthly totals for insightSummary.
        /// Sorts months chronologically.
        /// </summary>
        private void ComputeMonthlyTotals(IEnumerable<Row> rows, InsightsRoot root)
        {
            var uniqueMonths = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.MonthYear))
                .Select(r => r.MonthYear)
                .Distinct()
                .ToList();

            var sortedMonths = new List<(string Month, DateTime Date)>();

            foreach (var monthStr in uniqueMonths)
            {
                if (DateTime.TryParseExact(monthStr, "MMM yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    sortedMonths.Add((monthStr, new DateTime(parsedDate.Year, parsedDate.Month, 1)));
                }
            }

            sortedMonths = sortedMonths.OrderBy(m => m.Date).ToList();

            for (int i = 0; i < sortedMonths.Count; i++)
            {
                var month = sortedMonths[i].Month;
                var monthGroup = rows.Where(r => r.MonthYear == month).ToList();

                var inTotal = monthGroup.Where(r => r.Type.ToLowerInvariant() == "in").Sum(r => r.Amount);
                var outTotal = monthGroup.Where(r => r.Type.ToLowerInvariant() == "out").Sum(r => r.Amount);

                root.InsightSummary.Add(new MonthlyInsight
                {
                    Index = i + 1,
                    Month = month,
                    Summary = new MonthlySummary
                    {
                        MoneyInTotal = inTotal,
                        MoneyOutTotal = outTotal
                    }
                });
            }
        }
    }
}