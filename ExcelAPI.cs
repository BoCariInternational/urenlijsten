using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Font = DocumentFormat.OpenXml.Spreadsheet.Font;
using System.Linq;
using OpenXmlExtensions;

using DocProps = DocumentFormat.OpenXml.ExtendedProperties;
using Spreadsheet = DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.ExtendedProperties;
/*
 * private uint CreateStyle(
    WorkbookPart workbookPart,  // verplicht
    bool bold = false,          // optioneel (default false)
    bool italic = false,        // optioneel
    string fontName = null,     // optioneel
    double? fontSize = null,    // optioneel
    string format = null        // optioneel
)
 * 
 * 
 * 
 */


namespace OpenXmlExtensions
{
    public static class FontExtensions
    {
        /// <summary>
        /// Removes all empty/null elements from a Font while preserving structure
        /// </summary>
        public static Font Clean(this Font font)
        {
            if (font == null) return font;

            // List of elements that can be safely removed if empty
            var elementsToCheck = new OpenXmlElement[]
            {
                    font.Bold,
                    font.Italic,
                    font.Underline,
                    font.FontSize,
                    font.Color,
                    font.FontName,
                    font.FontFamilyNumbering,
                    font.FontScheme
            };

            foreach (var element in elementsToCheck.Where(e => e != null))
            {
                if (IsEmpty(element))
                {
                    element.Remove();
                }
            }

            return font;
        }

        private static bool IsEmpty(OpenXmlElement element)
        {
            return element switch
            {
                FontSize fs => fs.Val == null || fs.Val.Value == 0,
                FontName fn => string.IsNullOrEmpty(fn.Val?.Value),
                OpenXmlLeafElement leaf => string.IsNullOrEmpty(leaf.InnerText),
                _ => false
            };

        }
    }// static class OpenXmlElementExtensions
}//namespace OpenXmlExtensions

namespace Urenlijsten_App
{
    internal class ExcelAPI //: IDisposable
    {
        private SpreadsheetDocument? doc = null;
        private WorkbookPart workbookPart;
        private WorksheetPart worksheetPart;
        private SheetData sheetData;
        private bool _isDisposed = false;
        uint _customFormatId = 164; // Begin bij 164 voor custom format ID.

        public ExcelAPI(string filePath)
        {
            try
            {
                doc = SpreadsheetDocument.Open(filePath, true);
                if (doc.ExtendedFilePropertiesPart == null)
                {
                    doc.AddExtendedFilePropertiesPart();
                    doc.ExtendedFilePropertiesPart.Properties = new DocProps.Properties(
                        new DocProps.Application("Microsoft Excel"),
                        new DocProps.ApplicationVersion("14.0300") // Voor Excel 2010/2013

                        // Excel Versie    ApplicationVersion
                        //  2007            "12.0000"
                        //  2010            "14.0000"
                        //  2013            "15.0000"
                        //  2016            "16.0000"
                        //  2019 / 365      "16.0300"
                    );
                }
                workbookPart = doc.WorkbookPart;
                workbookPart.Workbook.WorkbookProperties = new Spreadsheet.WorkbookProperties
                {
                    CodeName = "ThisWorkbook",
                    DefaultThemeVersion = 124226
                };
                if (!workbookPart.Workbook.NamespaceDeclarations.Any())
                {
                    workbookPart.Workbook.AddNamespaceDeclaration("r",
                        "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                }

                /*
                if (workbookPart.WorkbookSettingsPart == null)
                {
                    var settingsPart = doc.WorkbookPart.AddNewPart<WorkbookSettingsPart>();
                    settingsPart.Settings = new Spreadsheet.WorkbookSettings(
                        new Spreadsheet.BookViews(
                            new Spreadsheet.WorkbookView()
                        )
                    );
                }
                */

                // Ensure worksheet exists
                if (workbookPart.Workbook.Sheets.Count() == 0)
                {
                    throw new InvalidOperationException("Workbook contains no worksheets");
                }

                var sheet = workbookPart.Workbook.Sheets.Elements<Sheet>().First();
                worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                //var worksheetPart = doc.WorkbookPart.WorksheetParts.First();

                // Ensure worksheet has SheetData
                if (worksheetPart.Worksheet.Elements<SheetData>().Count() == 0)
                {
                    worksheetPart.Worksheet.Append(new SheetData());
                }
                if (!worksheetPart.Worksheet.NamespaceDeclarations.Any())
                {
                    worksheetPart.Worksheet.AddNamespaceDeclaration("",
                        "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                }

                sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData == null)
                    throw new ArgumentNullException(nameof(sheetData));
            }
            catch (Exception ex)
            {
                doc?.Dispose();
                throw;  // Re-throw to let caller handle
            }
        }

        public void ModifyExcel()
        {
            // A1: Styling tekst
            SetCellValue("A11", "Bold Italic Text",
                CreateStyle(bold: true, italic: true, fontName: "Arial", fontSize: 14));

            // A2: Datum met formaat
            SetCellValue("A12", DateTime.Now,
                CreateStyle(format: "mm/dd/yyyy"));

            // A3: Integer
            SetCellValue("A13", 42);

            // A4: Float met 2 decimalen
            SetCellValue("A14", 123.456,
                CreateStyle(format: "0.00"));

            // A5: Formule
            SetCellValue("A15", formula: "SUM(A3:A4)");
        }

        /*
        public void WriteBasicValue(string address, object value)
        {
            var cell = GetOrCreateCell(address);
            cell.CellValue = new CellValue(value.ToString());
            cell.DataType = value switch
            {
                int _ => CellValues.Number,
                double _ => CellValues.Number,
                DateTime _ => CellValues.Date,
                _ => CellValues.String
            };
        }
        */

        private void SetCellValue(string address, object value = null,
            uint? styleIndex = null, string formula = null)
        {
            var rowIndex = GetRowIndex(address);

            // Find or create row
            var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex);
            if (row == null)
            {
                row = new Row() { RowIndex = rowIndex };
                sheetData.Append(row);
            }

            // Find or create cell
            var cell = row.Elements<Cell>().FirstOrDefault(c => c.CellReference == address);
            if (cell == null)
            {
                cell = new Cell() { CellReference = address };
                row.Append(cell);
            }
            else
            {
                // Clear existing cell contents
                cell.RemoveAllChildren();
            }

            if (formula != null)
                cell.CellFormula = new CellFormula(formula);

            if (value != null)
            {
                if (value is DateTime)
                {
                    cell.DataType = CellValues.Number;
                    cell.CellValue = new CellValue(((DateTime)value).ToOADate().ToString());
                }
                else if (value is int || value is double)
                {
                    cell.DataType = CellValues.Number;
                    cell.CellValue = new CellValue(value.ToString());
                }
                else
                {
                    cell.DataType = CellValues.String;
                    cell.CellValue = new CellValue(value.ToString());
                }
            }

            if (styleIndex.HasValue)
                cell.StyleIndex = styleIndex.Value;

            //row.Append(cell);
            //sheetData.Append(row);
        }

        private uint CreateStyle(bool bold = false, bool italic = false,
            string fontName = null, double? fontSize = null, string format = null)
        {
            var stylesPart = workbookPart.WorkbookStylesPart ??
                            workbookPart.AddNewPart<WorkbookStylesPart>();

            // Initialize stylesheet if new
            if (stylesPart.Stylesheet == null)
            {
                stylesPart.Stylesheet = new Stylesheet(
                    new Fonts(new Font()), // Default font
                    new Fills(new Fill()), // Default fill
                    new Borders(new Border()), // Default border
                    new CellFormats(new CellFormat()) // Default format
                );
            }

            // Check for existing identical style
            uint? existingStyleId = FindExistingStyle(stylesPart.Stylesheet, bold, italic, fontName, fontSize, format);
            if (existingStyleId.HasValue)
            {
                return existingStyleId.Value;
            }

            // --- Font Handling ---
            var fonts = stylesPart.Stylesheet.Fonts ?? new Fonts();
            var font = new Font
            {
                Bold = bold ? new Bold() : null,
                Italic = italic ? new Italic() : null,
                FontName = fontName != null ? new FontName { Val = fontName } : null,
                FontSize = fontSize.HasValue ? new FontSize { Val = fontSize.Value } : null
            }.Clean(); // Extension method to remove null properties

            fonts.Append(font);
            stylesPart.Stylesheet.Fonts = fonts;

            // --- Number Format Handling ---
            uint formatId = 0; // General format by default
            if (format != null)
            {
                var numberingFormats = stylesPart.Stylesheet.NumberingFormats ??
                                     new NumberingFormats { Count = 0 };

                // Check if format already exists
                var existingFormat = numberingFormats
                    .OfType<NumberingFormat>()
                    .FirstOrDefault(nf => nf.FormatCode == format);

                if (existingFormat != null)
                {
                    formatId = existingFormat.NumberFormatId;
                }
                else
                {
                    formatId = _customFormatId++;
                    numberingFormats.Append(new NumberingFormat
                    {
                        NumberFormatId = formatId,
                        FormatCode = format
                    });
                    numberingFormats.Count++;
                    stylesPart.Stylesheet.NumberingFormats = numberingFormats;
                }
            }

            // --- Cell Format Creation ---
            var cellFormats = stylesPart.Stylesheet.CellFormats ?? new CellFormats();
            var cellFormat = new CellFormat
            {
                FontId = (uint)(fonts.Count - 1),
                ApplyFont = BooleanValue.FromBoolean(true),
                NumberFormatId = formatId,
                ApplyNumberFormat = BooleanValue.FromBoolean(format != null)
            };

            cellFormats.Append(cellFormat);
            stylesPart.Stylesheet.CellFormats = cellFormats;

            //stylesPart.Stylesheet.Save();
            //We can only save after removing namespaces

            return (uint)(cellFormats.Count() - 1); // Returns the index of the newly added format
        }

        // Helper to find existing matching style
        private uint? FindExistingStyle(Stylesheet stylesheet, bool bold, bool italic,
            string fontName, double? fontSize, string format)
        {
            // Implementation would compare all style properties
            // against existing ones in the stylesheet
            // Return matching style index if found
            return null;
        }

        private uint GetRowIndex(string address)
        {
            var rowPart = new string(address.SkipWhile(c => !char.IsDigit(c))
                              .TakeWhile(char.IsDigit)
                              .ToArray());
            return uint.Parse(new string(rowPart));
        }

        // Method om op te slaan
        public void Save()
        {
            if (doc != null && !_isDisposed)
            {
                RemoveNamespacePrefixes(worksheetPart.Worksheet);
                worksheetPart.Worksheet.Save();     // Saves worksheet XML

                RemoveNamespacePrefixes(doc.WorkbookPart.Workbook);
                doc.WorkbookPart.Workbook.Save();   // Updates workbook references


                // Ensure WorkbookStylesPart exists and save it
                if (workbookPart.WorkbookStylesPart != null)
                {
                    workbookPart.WorkbookStylesPart.Stylesheet.Save();
                }

                doc.Save();                         // Finalizes package
            }
        }

        void RemovePrefixes(OpenXmlElement element)
        {
            foreach (var child in element.Descendants())
            {
                if (child.Prefix == "x") child.Prefix = "";
            }
        }

        // IDisposable implementatie
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing && doc != null)
                {
                    Save(); // Auto-save bij dispose
                    doc.Dispose();
                }
                _isDisposed = true;
            }
        }

        // Hulpmethoden
        private WorksheetPart GetWorksheetPart()
        {
            return doc.WorkbookPart.WorksheetParts.First();
        }
    }// class ExcelAPI
}//namespace Urenlijsten_App
