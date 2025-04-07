using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;

public class ProjectParser
{
    public class Project
    {
        public int Code { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }

    public void ParseAndExport(string excelPath, string jsonPath)
    {
        try
        {
            var projects = new List<Project>();
            var typeColorMap = new Dictionary<string, string>();
            int? prevCode = null;

            using (var workbook = new XLWorkbook(excelPath))
            {
                var worksheet = workbook.Worksheet(1);
                int headerRow = FindHeaderRow(worksheet);

                for (int row = headerRow + 1; !worksheet.Cell(row, 1).IsEmpty(); row++)
                {
                    // Parse project data
                    var project = new Project
                    {
                        Code = worksheet.Cell(row, 1).GetValue<int>(),
                        Type = worksheet.Cell(row, 2).GetString(),
                        Description = worksheet.Cell(row, 3).GetString()
                    };

                    // Check sorting
                    if (prevCode.HasValue && project.Code < prevCode)
                    {
                        Console.WriteLine($"Warning: Projects not sorted at code {project.Code}");
                        break;
                    }
                    prevCode = project.Code;

                    // Get current cell color
                    string currentColor = GetCellColor(worksheet.Cell(row, 2));

                    // Handle color mapping
                    if (!typeColorMap.TryGetValue(project.Type, out string expectedColor))
                    {
                        // First occurrence - store the color
                        typeColorMap[project.Type] = currentColor;
                    }
                    else if (expectedColor != currentColor)
                    {
                        Console.WriteLine($"Warning: Inconsistent color for type '{project.Type}'. " +
                                        $"Expected: {expectedColor}, Found: {currentColor} " +
                                        $"at project code {project.Code}");
                    }

                    projects.Add(project);
                }
            }

            // Prepare final output
            var result = new
            {
                Projects = projects,
                TypeMetadata = typeColorMap.ToDictionary(
                    kv => kv.Key,
                    kv => new { Color = kv.Value })
            };

            // Export JSON
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(result, Formatting.Indented));
            Console.WriteLine($"Successfully exported {projects.Count} projects to {jsonPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private string GetCellColor(IXLCell cell)
    {
        var color = cell.Style.Fill.BackgroundColor;
        return color == XLColor.NoColor ? "#FFFFFF" :
            $"#{color.Color.R:X2}{color.Color.G:X2}{color.Color.B:X2}";
    }

    private int FindHeaderRow(IXLWorksheet worksheet)
    {
        for (int row = 1; row <= 10; row++)
        {
            if (worksheet.Cell(row, 1).GetString().Equals("Project Code", StringComparison.OrdinalIgnoreCase))
            {
                return row;
            }
        }
        return -1;
    }
}