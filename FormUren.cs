using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using CustomControls;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Draw = System.Drawing;

namespace Urenlijsten_App
{
    public static class DataGridViewExtensions
    {
        public static int GetColumnIndexByHeader(this DataGridView dataGridView, string headerText)
        {
            for (int i = 0; i < dataGridView.Columns.Count; i++)
            {
                if (dataGridView.Columns[i].HeaderText != null && dataGridView.Columns[i].HeaderText.StartsWith(headerText, StringComparison.OrdinalIgnoreCase))
                {
                    return i; // Het kolomnummer (de index) is gevonden
                }
            }
            return -1; // De kolom met de opgegeven header is niet gevonden
        }
    }
    public class FormUren : Form
    {
        public static Image imageCalendar, imageAdd, imageDelete, imageEdit, imageUndo, imageOk;
        public PanelLogo panelLogoNaam;
        public PanelUren panelUren;
        public Panel panelVerlof;

        private Image LoadIcon(string fileName)
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, @"..\..\..\icons", fileName);
                path = Path.GetFullPath(path);  // normalized path without \..\
                return Image.FromFile(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading icon {fileName}: {ex.Message}");
                return new Bitmap(32, 32); // Return empty image as fallback
            }
        }

        public FormUren()
        {
            Initialize(); // Jouw eigen methode
        }

        private void Initialize()
        {
            this.Text = "Urenlijst";
            this.Size = new Size(1000, 1000); // Startgrootte

            // Load all icons (they're already 32x32)
            imageCalendar = LoadIcon("calendar_400959.png");
            imageAdd = LoadIcon("add.png");
            imageDelete = LoadIcon("trash.png");
            imageEdit = LoadIcon("note.png");
            imageUndo = LoadIcon("icons8-undo-30.png"); // Will work even if 30x30
            imageOk = LoadIcon("check.png");

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                AutoSize = true
            };

            // Zorg ervoor dat de eerste kolom alles opvult
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Voeg rijen toe aan de tabel
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Logo-Naam
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));  // Spacer-1
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 60));  // Uren (grootste deel)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));  // Spacer-2
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 30));  // Verlof
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));  // Spacer-3
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Submit (vaste hoogte)

            // Panels maken
            panelLogoNaam = new PanelLogo();
            panelUren = new PanelUren();
            panelVerlof = CreatePanel(Draw.Color.LightCoral, "Verlof");

            //RR!! test code
            Panel p = new()
            {
                Width = 600,
                Dock = DockStyle.Left,
                BackColor = Draw.Color.AliceBlue
            };
            Button b1 = new Button()
            {
                //Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Dock = DockStyle.Left,
                Text = "b1"
            };
            p.Controls.Add(b1);

            CheckedComboBox cb = new()
            {
                Dock = DockStyle.Left,
                Width = 500
            };
            var shortableProjectTypes = panelUren.projectDataJson.projectTypes.ConvertAll(type => new PanelUren.ShortableProjectType { TypeName = type });
            cb.SetDataSource(shortableProjectTypes);
            p.Controls.Add(cb);

            Button b2 = new Button()
            {
                Dock = DockStyle.Left,
                Text = "b2"
            };
            p.Controls.Add(b2);
            panelVerlof.Controls.Add(p);

            var ncb = new ComboBox()
            {
                Dock = DockStyle.Left,
                Text = "Blah"
            };
            panelVerlof.Controls.Add(ncb);

            // RR! end test code

            var panelSubmit = CreatePanel(Draw.Color.Gray, "Submit");
            //panelSubmit.Height = 40;

            // Spacers (lege panelen met vaste hoogte)
            var spacer1 = CreateSpacer();
            var spacer2 = CreateSpacer();
            var spacer3 = CreateSpacer();

            var btnSumbit = new Button()
            {
                Text = "Submit",
                Width = 100,
                Dock = DockStyle.Right,
            };
            panelSubmit.Controls.Add(btnSumbit);
            btnSumbit.Click += BtnSubmit_Click;

            // Toevoegen aan de tabel
            table.Controls.Add(panelLogoNaam, 0, 0);
            table.Controls.Add(spacer1, 0, 1);
            table.Controls.Add(panelUren, 0, 2);
            table.Controls.Add(spacer2, 0, 3);
            table.Controls.Add(panelVerlof, 0, 4);
            table.Controls.Add(spacer3, 0, 5);
            table.Controls.Add(panelSubmit, 0, 6);

            this.Controls.Add(table);
        }

        private Panel CreatePanel(Draw.Color color, string text)
        {
            return new Panel
            {
                BackColor = color,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Controls = { new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter } }
            };
        }

        private Panel CreateSpacer()
        {
            return new Panel
            {
                Dock = DockStyle.Fill
            };
        }

        private void Assign(IXLCell cell, object gridValue)
        {
            if (gridValue != null)
            {
                if (gridValue is int intValue)
                {
                    cell.Value = intValue;
                }
                else if (gridValue is float floatValue)
                {
                    cell.Value = floatValue;
                }
                else if (gridValue is double doubleValue) // Overweeg ook double
                {
                    cell.Value = doubleValue;
                }
                else if (gridValue is string stringValue)
                {
                    cell.Value = stringValue;
                }
                else if (gridValue is DateTime dateTimeValue)
                {
                    cell.Value = dateTimeValue;
                }
                else
                {
                    // Als het een ander type is, kun je het eventueel als string opslaan
                    cell.Value = gridValue.ToString();
                }
            }
        }// Assign

        // Zoek header in kopie van template die we aan het schrijven zijn.
        public int FindColumn(IXLWorksheet worksheet, string headerText, int row)
        {
            for (int col = 1; col < 20; ++col)
            {
                string cellText = worksheet.Cell(row, col).Value.ToString();
                if (cellText.StartsWith(headerText, StringComparison.OrdinalIgnoreCase))
                    return col;
            }
            return -1;
        }

        private void CopyColumnProjectUren(string headerText, IXLWorksheet worksheet, DataGridView dv)
        {
            const int maxRows = 10;
            const int startHeaderTemplate = 8;
            //kopieer kolom met headerText:
            int colDataGrid = dv.GetColumnIndexByHeader(headerText);
            int colExcel = FindColumn(worksheet, headerText, startHeaderTemplate);
            if (colDataGrid != -1 && colExcel != -1)
            {
                for (int row = 0; row < maxRows; ++row)
                {
                    Assign(worksheet.Cell(row + startHeaderTemplate + 1, colExcel), dv.Rows[row].Cells[colDataGrid].Value);
                }
            }
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            // 1) Maak een kopie van het sjabloon
            string templatePath = Path.Combine(Application.StartupPath, @"..\..\..\UrenlijstBLS.xlsx");
            templatePath = Path.GetFullPath(templatePath);  // normalized path without \..\
            string destionationPath = Path.GetDirectoryName(templatePath);
            string destinationPath = Path.Combine(destionationPath, "result.xlsx");

            try
            {
                File.Copy(templatePath, destinationPath, true); // true allows overwriting if the file exists
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij het kopiëren van het sjabloon: {ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2) Open de kopie met ClosedXML
            try
            {
                using (var workbook = new XLWorkbook(destinationPath))
                {
                    var worksheet = workbook.Worksheets.First(); // Ga ervan uit dat de data in het eerste werkblad moet komen
                    var dv = this.panelUren.dataGridView1;

                    //const int maxRows = 10;

                    CopyColumnProjectUren("Klantnaam", worksheet, dv);
                    CopyColumnProjectUren("km", worksheet, dv);
                    var dagen = new[] { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
                    foreach (var dag in dagen)
                    {
                        CopyColumnProjectUren(dag, worksheet, dv);
                    }


                    int col;
                    col = FindColumn(worksheet, "Naam", 2); worksheet.Cell(2, col + 1).Value = panelLogoNaam.txtName.Text;
                    col = FindColumn(worksheet, "Week", 4); worksheet.Cell(4, col + 1).Value = panelLogoNaam.txtWeek.Text;
                    col = FindColumn(worksheet, "van", 5); worksheet.Cell(5, col + 1).Value = $"{panelLogoNaam.ctrlWeek.Date} - {panelLogoNaam.lblTotDate.Text}";
                    worksheet.Cell("D1").Value = panelLogoNaam.lblCompany.Text;
                    //worksheet.Cell("N2").Value = panelLogoNaam.txtName.Text;
                    //worksheet.Cell("N4").Value = panelLogoNaam.txtWeek.Text;
                    //worksheet.Cell("N5").Value = $"{panelLogoNaam.ctrlWeek.Date} - {panelLogoNaam.lblTotDate.Text}";
                    //worksheet.Cell("D1").Value = panelLogoNaam.lblCompany.Text;

                    // Save de kopie.xlsx
                    workbook.Save();
                    MessageBox.Show($"Data succesvol geëxporteerd naar {destinationPath}", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij het schrijven naar Excel: {ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }



            /*
            try
            {
                ExcelAPI excel = new ExcelAPI(path);

                excel.ModifyExcel();
                excel.Save();

                MessageBox.Show("Formulier verzonden!", "Bevestiging", MessageBoxButtons.OK, MessageBoxIcon.Information);


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er is een fout opgetreden tijdens het maken van een excel sheet: {ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            */
        }// BtnSubmit_Click
    }//class FormUren
}
