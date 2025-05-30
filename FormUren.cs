﻿using System;
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
            foreach (DataGridViewColumn col in dataGridView.Columns)
            {
                if (col.HeaderText != null &&
                    col.HeaderText.StartsWith(headerText, StringComparison.OrdinalIgnoreCase))
                {
                    return col.Index;
                }
            }

            return -1; // De kolom met de opgegeven header is niet gevonden
        }

        public static bool IsInvalidCell(this DataGridViewCellEventArgs e)
        {
            return e == null || e.RowIndex < 0 || e.ColumnIndex < 0;
        }

        public static bool IsInvalidCell(this DataGridViewCellCancelEventArgs e)
        {
            return e == null || e.RowIndex < 0 || e.ColumnIndex < 0;
        }

        public static bool IsInvalidCell(this DataGridViewCellFormattingEventArgs e)
        {
            return e == null || e.RowIndex < 0 || e.ColumnIndex < 0;
        }

        public static bool IsInvalidCell(this DataGridViewCellValidatingEventArgs e)
        {
            return e == null || e.RowIndex < 0 || e.ColumnIndex < 0;
        }
    }

    public class FormUren : Form
    {
        public static Image imageCalendar, imageAdd, imageDelete, imageEdit, imageUndo, imageOk;
        public PanelLogo panelLogoNaam;
        public PanelUren panelUren;
        public Panel panelVerlof;
        public static FormUren Current { get; private set; }

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

        CheckedComboBox cb;

        public FormUren()
        {
            Current = this;
            Initialize(); // Jouw eigen methode

            var shortableProjectTypes = panelUren.projectDataJson.projectTypes.ConvertAll(type => new PanelUren.ShortableProjectType { TypeName = type });
            //RR!cb.SetDataSource(shortableProjectTypes);
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
                Width = 800,
                Dock = DockStyle.Left,
                BackColor = Draw.Color.SkyBlue,
            };
            Button b1 = new Button()
            {
                //Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Dock = DockStyle.Left,
                Text = "b1"
            };
            p.Controls.Add(b1);

            cb = new()
            {
                Dock = DockStyle.Left,
                Width = 500
            };
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
                Text = "Blah",
                DataSource = new List<string> { "11", "12", "13", "21", "22", "23" },
                AutoCompleteMode = AutoCompleteMode.None,
                AutoCompleteSource = AutoCompleteSource.None,
            };
            panelVerlof.Controls.Add(ncb);
            ncb.SelectionChangeCommitted += VerlofSelectionChangeCommitted;

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

        public static void Assign(IXLCell cell, object gridValue)
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
                    cell.Value = stringValue.Trim();
                }
                else if (gridValue is DateTime dateTimeValue)
                {
                    cell.Value = dateTimeValue;
                }
                else
                {
                    // Als het een ander type is, kun je het eventueel als string opslaan
                    // cell.Value = gridValue.ToString();
                }
            }
        }// Assign

        const int rowHeaderInTemplate = 8;
        const int maxRows = 10;


        // ComputeTotal2

        // Zoek header in kopie van template die we aan het schrijven zijn.
        public static int FindColumn(IXLWorksheet worksheet, string headerText, int rowInTemplate)
        {
            for (int col = 1; col < 30; ++col)
            {
                string cellText = worksheet.Cell(rowInTemplate, col).Value.ToString();
                if (cellText.StartsWith(headerText, StringComparison.OrdinalIgnoreCase))
                    return col;
            }
            return -1;
        }

        private void VerlofSelectionChangeCommitted(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // Hier kun je de geselecteerde waarde gebruiken
                string selectedValue = comboBox.SelectedItem.ToString();
                MessageBox.Show($"Geselecteerde waarde: {selectedValue}");
            }
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            // 1) Maak een kopie van het sjabloon
            string templatePath = Path.Combine(Application.StartupPath, @"..\..\..\BoCariUrenlijstTemplate.xlsx");
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

                    panelLogoNaam.OnSubmit(worksheet);
                    panelUren.OnSubmit(worksheet);

                    // Save de kopie.xlsx
                    workbook.Save();
                    MessageBox.Show($"Data succesvol geëxporteerd naar {destinationPath}", "Succes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij het schrijven naar Excel: {ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }// BtnSubmit_Click
    }//class FormUren
}
