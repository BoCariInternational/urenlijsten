using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Font = DocumentFormat.OpenXml.Spreadsheet.Font;

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

namespace Urenlijsten_App
{


    internal class ExcelAPI
    {
        private SpreadsheetDocument? doc = null;
        private WorkbookPart workbookPart;
        private WorksheetPart worksheetPart;
        private SheetData sheetData;
        private bool _isDisposed = false;
        uint formatId = 164; // Begin bij custom format ID

        ExcelAPI(string filePath)
        {
            try
            {
                doc = SpreadsheetDocument.Open(filePath, true);
                workbookPart = doc.WorkbookPart;
                var sheet = workbookPart.Workbook.Sheets
                    .Elements<Sheet>()
                    .FirstOrDefault();

                //var sheet = doc.WorkbookPart.Workbook.Descendants<Sheet>()
                //           .First(s => s.Name.Equals("Sheet1"));

                // SheetData (inhoud) opvragen
                WorksheetPart worksheetPart = (WorksheetPart)doc.WorkbookPart
                                            .GetPartById(sheet.Id);
                SheetData sheetData = worksheetPart.Worksheet
                                  .GetFirstChild<SheetData>();
            }
            catch (Exception ex)
            {
                doc = null;
                // Optioneel: logging toevoegen
                Console.WriteLine($"Fout bij openen bestand: {ex.Message}");
            }
        }

        public void ModifyExcel()
        {
            // A1: Styling tekst
            SetCellValue("A1", "Bold Italic Text",
                CreateStyle(bold: true, italic: true, fontName: "Arial", fontSize: 14));

            // A2: Datum met formaat
            SetCellValue("A2", DateTime.Now,
                CreateStyle(format: "mm/dd/yyyy"));

            // A3: Integer
            SetCellValue("A3", 42);

            // A4: Float met 2 decimalen
            SetCellValue("A4", 123.456,
                CreateStyle(format: "0.00"));

            // A5: Formule
            SetCellValue("A5", formula: "SUM(A3:A4)");

            worksheetPart.Worksheet.Save();
        }

        private void SetCellValue(string address, object value = null,
            uint? styleIndex = null, string formula = null)
        {
            var cellReference = new StringValue(address);
            var rowIndex = GetRowIndex(address);

            var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex == rowIndex) ??
                     new Row() { RowIndex = rowIndex };

            var cell = new Cell() { CellReference = cellReference };

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

            row.Append(cell);
            sheetData.Append(row);
        }

        private uint CreateStyle(bool bold = false, bool italic = false,
            string fontName = null, double? fontSize = null, string format = null)
        {
            var stylesPart = workbookPart.WorkbookStylesPart ??
                            workbookPart.AddNewPart<WorkbookStylesPart>();

            if (stylesPart.Stylesheet == null)
                stylesPart.Stylesheet = new Stylesheet();

            var fonts = stylesPart.Stylesheet.Fonts ?? new Fonts();
            var font = new Font();

            if (bold) font.Append(new Bold());
            if (italic) font.Append(new Italic());
            if (fontName != null) font.Append(new FontName() { Val = fontName });
            if (fontSize.HasValue) font.Append(new FontSize() { Val = fontSize.Value });

            fonts.Append(font);
            stylesPart.Stylesheet.Fonts = fonts;

            var numberingFormats = stylesPart.Stylesheet.NumberingFormats ??
                                 new NumberingFormats() { Count = 0 };

            if (format != null)
            {
                numberingFormats.Append(new NumberingFormat
                {
                    NumberFormatId = formatId++,
                    FormatCode = format
                });
                numberingFormats.Count++;
            }

            stylesPart.Stylesheet.NumberingFormats = numberingFormats;

            var cellFormats = stylesPart.Stylesheet.CellFormats ?? new CellFormats();
            var cellFormat = new CellFormat
            {
                FontId = (uint)(fonts.Count - 1),
                ApplyFont = true
            };

            if (format != null)
            {
                cellFormat.NumberFormatId = formatId - 1;
                cellFormat.ApplyNumberFormat = true;
            }

            cellFormats.Append(cellFormat);
            stylesPart.Stylesheet.CellFormats = cellFormats;

            stylesPart.Stylesheet.Save();

            return (uint)(cellFormats.Count - 1);
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
                doc.WorkbookPart.Workbook.Save();
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
    }
}
