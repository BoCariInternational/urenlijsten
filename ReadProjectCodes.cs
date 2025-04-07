using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Urenlijsten_App
{
    public enum ProjectTypeEnum
    {
        Unknown,
        Process,
        Mechanical,
        Piping,
        Civil,
        Structural,
        Architectural,
        Instrumentation,
        Automation,
        Electrical,
        Multiple,
        ProjectManagement, // "Project Management, Admin & Controls"
        IT_OT,             // "IT/OT"
        Other
    }

    public class Project
    {
        public int Code { get; set; }
        public ProjectTypeEnum ProjectType { get; set; }
        public string Description { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "projects.xlsx");

                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1);

                    // Find header row by searching for "Project Code" in column 1
                    int headerRow = FindHeaderRow(worksheet, "Project Code", 1, 10);

                    if (headerRow == -1)
                    {
                        Console.WriteLine("Error: Could not find header row");
                        return;
                    }

                    var projects = new List<Project>();
                    int currentRow = headerRow + 1;

                    while (!worksheet.Cell(currentRow, 1).IsEmpty())
                    {
                        try
                        {
                            var project = ParseProjectRow(worksheet, currentRow);
                            if (project != null)
                            {
                                projects.Add(project);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing row {currentRow}: {ex.Message}");
                        }
                        currentRow++;
                    }

                    Console.WriteLine($"Successfully parsed {projects.Count} projects:");
                    foreach (var project in projects.Take(10)) // Show first 10 as sample
                    {
                        Console.WriteLine($"{project.Code,-8} {project.ProjectType,-15} {project.Description}");
                    }
                    if (projects.Count > 10)
                        Console.WriteLine($"... and {projects.Count - 10} more projects");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
            }
        }

        static int FindHeaderRow(IXLWorksheet worksheet, string headerText, int column, int maxRowsToCheck)
        {
            for (int row = 1; row <= maxRowsToCheck; row++)
            {
                if (worksheet.Cell(row, column).GetString().Equals(headerText, StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }
            return -1;
        }

        static Project ParseProjectRow(IXLWorksheet worksheet, int row)
        {
            // Get cell values
            string codeStr = worksheet.Cell(row, 1).GetString();
            string typeStr = worksheet.Cell(row, 2).GetString().Trim();
            string description = worksheet.Cell(row, 3).GetString();

            // Parse project code
            if (!int.TryParse(codeStr, out int code) || codeStr.Length < 5 || codeStr.Length > 6)
            {
                Console.WriteLine($"Invalid project code at row {row}: {codeStr}");
                return null;
            }

            return new Project
            {
                Code = code,
                ProjectType = ParseProjectType(typeStr),
                Description = description
            };
        }

        static ProjectTypeEnum ParseProjectType(string typeStr)
        {
            if (string.IsNullOrWhiteSpace(typeStr))
                return ProjectTypeEnum.Unknown;

            // Normalize the string
            string normalized = typeStr
                .Replace(",", "")
                .Replace("&", "")
                .Replace("/", "_")
                .Replace(" ", "")
                .Trim()
                .ToLower();

            return normalized switch
            {
                "process" => ProjectTypeEnum.Process,
                "mechanical" => ProjectTypeEnum.Mechanical,
                "piping" => ProjectTypeEnum.Piping,
                "civil" => ProjectTypeEnum.Civil,
                "structural" => ProjectTypeEnum.Structural,
                "architectural" => ProjectTypeEnum.Architectural,
                "instrumentation" => ProjectTypeEnum.Instrumentation,
                "automation" => ProjectTypeEnum.Automation,
                "electrical" => ProjectTypeEnum.Electrical,
                "multiple" => ProjectTypeEnum.Multiple,
                var s when s.Contains("projectmanagementadmincontrols") => ProjectTypeEnum.ProjectManagement,
                "it_ot" or "itot" => ProjectTypeEnum.IT_OT,
                _ => ProjectTypeEnum.Other
            };
        }
    }
}
