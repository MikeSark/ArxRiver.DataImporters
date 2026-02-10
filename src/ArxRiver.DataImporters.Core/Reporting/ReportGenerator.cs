using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArxRiver.DataImporters.Core.Validation;
using TextCopy;

namespace ArxRiver.DataImporters.Core.Reporting;

/// <summary>
/// Generates validation reports in HTML or JSON format, output to file or clipboard.
/// Rows are tracked with their original row numbers for accurate error correlation.
/// </summary>
public sealed class ReportGenerator<T> where T : class
{
    private readonly IReadOnlyList<(T Item, int RowNumber)> _allRows;
    private readonly ReadOnlyCollection<ValidationResult> _validationResults;
    private readonly Dictionary<int, List<ValidationResult>> _errorsByRowNumber;
    private readonly HashSet<int> _invalidRowNumbers;

    public enum RowFilter
    {
        Valid,
        Invalid,
        All
    }

    public ReportGenerator(
        IReadOnlyList<(T Item, int RowNumber)> allRows,
        ReadOnlyCollection<ValidationResult> validationResults)
    {
        _allRows = allRows;
        _validationResults = validationResults;
        _invalidRowNumbers = new HashSet<int>(validationResults.Select(v => v.RowNumber));
        _errorsByRowNumber = validationResults
            .GroupBy(v => v.RowNumber)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public void Generate(
        ReportFormat format,
        ReportDestination destination,
        RowFilter filter = RowFilter.All,
        string? outputFilePath = null)
    {
        var content = format switch
                      {
                          ReportFormat.Json => GenerateJson(filter),
                          ReportFormat.Html => GenerateHtml(filter),
                          _ => throw new ArgumentOutOfRangeException(nameof(format))
                      };

        switch (destination)
        {
            case ReportDestination.File:
                var path = outputFilePath
                           ?? $"report_{DateTime.Now:yyyyMMdd_HHmmss}.{(format == ReportFormat.Html ? "html" : "json")}";
                File.WriteAllText(path, content);
                Console.WriteLine($"Report written to: {Path.GetFullPath(path)}");
                break;

            case ReportDestination.Clipboard:
                ClipboardService.SetText(content);
                Console.WriteLine("Report copied to clipboard.");
                break;
        }
    }

    private string GenerateJson(RowFilter filter)
    {
        var rows = GetFilteredRows(filter);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var errorsByRule = _validationResults
            .GroupBy(v => v.RuleName)
            .Select(g => new
            {
                Rule = g.Key,
                Count = g.Count(),
                Failures = g.Select(v => new
                {
                    v.RowNumber,
                    v.PropertyName,
                    v.ErrorMessage
                })
            });

        var rowData = rows.Select(r =>
            {
                var dict = new Dictionary<string, object?> { ["ExcelRow"] = r.RowNumber };
                foreach (var prop in properties)
                    dict[prop.Name] = prop.GetValue(r.Item);

                var isInvalid = _invalidRowNumbers.Contains(r.RowNumber);
                dict["Status"] = isInvalid ? "Invalid" : "Valid";

                if (isInvalid && _errorsByRowNumber.TryGetValue(r.RowNumber, out var errors))
                {
                    dict["Errors"] = errors.Select(e => new { e.RuleName, e.PropertyName, e.ErrorMessage });
                }

                return dict;
            });

        var validCount = _allRows.Count(r => !_invalidRowNumbers.Contains(r.RowNumber));
        var invalidCount = _allRows.Count - validCount;

        var report = new
        {
            Summary = new
            {
                TotalRows = _allRows.Count,
                ValidRows = validCount,
                InvalidRows = invalidCount,
                TotalErrors = _validationResults.Count,
                RulesEvaluated = _validationResults.Select(v => v.RuleName).Distinct().Count()
            },
            ValidationErrorsByRule = errorsByRule,
            Rows = rowData
        };

        return JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    private string GenerateHtml(RowFilter filter)
    {
        var rows = GetFilteredRows(filter);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var validCount = _allRows.Count(r => !_invalidRowNumbers.Contains(r.RowNumber));
        var invalidCount = _allRows.Count - validCount;

        var sb = new StringBuilder();
        sb.AppendLine("""
                      <!DOCTYPE html>
                      <html lang="en">
                      <head>
                      <meta charset="UTF-8">
                      <title>Import Validation Report</title>
                      <style>
                        * { box-sizing: border-box; }
                        body { font-family: 'Segoe UI', system-ui, sans-serif; margin: 2rem; background: #f9fafb; color: #1a1a1a; }
                        h2 { color: #2563eb; margin-bottom: 0.5rem; }
                        h3 { color: #374151; }
                        .summary { display: flex; gap: 1rem; margin-bottom: 1.5rem; flex-wrap: wrap; }
                        .summary-card { background: white; border-radius: 8px; padding: 1rem 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.1); min-width: 140px; }
                        .summary-card .label { font-size: 0.85rem; color: #6b7280; }
                        .summary-card .value { font-size: 1.5rem; font-weight: 600; }
                        .value.total { color: #2563eb; }
                        .value.valid { color: #16a34a; }
                        .value.invalid { color: #dc2626; }
                        table { border-collapse: collapse; width: 100%; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
                        th { background: #2563eb; color: white; padding: 10px 12px; text-align: left; font-size: 0.9rem; }
                        td { border-bottom: 1px solid #e5e7eb; padding: 8px 12px; font-size: 0.9rem; }
                        tr.invalid-row { background: #fef2f2; }
                        tr.valid-row:hover, tr.invalid-row:hover { background: #f3f4f6; }
                        .error-list { color: #dc2626; font-size: 0.8rem; margin: 0; padding-left: 1rem; }
                        .rule-group { background: white; border-radius: 8px; padding: 1rem; margin-bottom: 0.5rem; box-shadow: 0 1px 2px rgba(0,0,0,0.05); }
                        .rule-name { font-weight: 600; color: #dc2626; }
                        .rule-count { color: #6b7280; font-size: 0.85rem; }
                      </style>
                      </head>
                      <body>
                      """);

        sb.AppendLine("<h2>Import Validation Report</h2>");
        sb.AppendLine($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"""
                       <div class="summary-card"><div class="label">Total Rows</div><div class="value total">{_allRows.Count}</div></div>
                       <div class="summary-card"><div class="label">Valid</div><div class="value valid">{validCount}</div></div>
                       <div class="summary-card"><div class="label">Invalid</div><div class="value invalid">{invalidCount}</div></div>
                       <div class="summary-card"><div class="label">Total Errors</div><div class="value invalid">{_validationResults.Count}</div></div>
                       """);
        sb.AppendLine("</div>");

        // Errors grouped by rule
        if (_validationResults.Count > 0)
        {
            sb.AppendLine("<h3>Validation Errors by Rule</h3>");
            foreach (var group in _validationResults.GroupBy(v => v.RuleName))
            {
                sb.AppendLine($"""<div class="rule-group"><span class="rule-name">{Escape(group.Key)}</span> <span class="rule-count">({group.Count()} failure(s))</span><ul class="error-list">""");
                foreach (var err in group)
                    sb.AppendLine($"<li>Row {err.RowNumber}, {Escape(err.PropertyName)}: {Escape(err.ErrorMessage)}</li>");
                sb.AppendLine("</ul></div>");
            }
        }

        // Data table
        sb.AppendLine("<h3>Data</h3>");
        sb.AppendLine("<table><thead><tr><th>Row#</th><th>Status</th>");
        foreach (var p in properties)
            sb.AppendLine($"<th>{Escape(p.Name)}</th>");
        sb.AppendLine("<th>Errors</th></tr></thead><tbody>");

        foreach (var (item, rowNum) in rows)
        {
            var isInvalid = _invalidRowNumbers.Contains(rowNum);
            var cssClass = isInvalid ? "invalid-row" : "valid-row";
            var status = isInvalid ? "Invalid" : "Valid";
            sb.Append($"<tr class=\"{cssClass}\"><td>{rowNum}</td><td>{status}</td>");

            foreach (var p in properties)
            {
                var val = p.GetValue(item);
                sb.Append($"<td>{Escape(val?.ToString() ?? "")}</td>");
            }

            sb.Append("<td>");
            if (_errorsByRowNumber.TryGetValue(rowNum, out var errors))
            {
                sb.Append("<ul class=\"error-list\">");
                foreach (var err in errors)
                    sb.Append($"<li>{Escape(err.RuleName)}: {Escape(err.ErrorMessage)}</li>");
                sb.Append("</ul>");
            }

            sb.AppendLine("</td></tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        return sb.ToString();
    }

    private IReadOnlyList<(T Item, int RowNumber)> GetFilteredRows(RowFilter filter) => filter switch
                                                                                        {
                                                                                            RowFilter.Valid => _allRows.Where(r => !_invalidRowNumbers.Contains(r.RowNumber)).ToList(),
                                                                                            RowFilter.Invalid => _allRows.Where(r => _invalidRowNumbers.Contains(r.RowNumber)).ToList(),
                                                                                            _ => _allRows.ToList()
                                                                                        };

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}