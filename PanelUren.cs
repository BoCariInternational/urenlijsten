using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CustomControls;
using System.ComponentModel;
using Newtonsoft.Json;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Draw = System.Drawing;
using Urenlijsten_App;
using System.Diagnostics;


// Vereiste interfaces
public interface IShortNameable
{
    string ToString();      // Lange naam voor checkboxes
    string ToStringShort(); // Korte naam voor textbox
}

namespace CustomControls
{

    public class PanelUren : Panel
    {
        public DataGridView dv;
        // In dv:
        private int colMa;
        private int colTotal;
        private int colKm;
        private const int rowTotal1 = 10;
        private const int rowSpacer = rowTotal1 + 1;
        private const int rowTotal2 = 20;

        // In (copy of) template (excel)
        private const int rowHeaderInTemplate = 8;
        private static System.Windows.Forms.Control editControl = null;


        // ProjectItem class met alle vereiste properties
        public class ProjectItem : IShortNameable
        {
            [JsonProperty("Code")]
            public int projectCode { get; set; }

            [JsonProperty("Type")]
            public string projectType { get; set; }

            [JsonIgnore]
            public int projectTypeInt { get; set; } // Hash of unieke ID voor het type

            [JsonProperty("Description")]
            public string description { get; set; }

            public string ToString() => $"{projectCode} - {description}";
            public string ToStringShort() => $"{projectCode}";
        }

        public class ProjectData  // root node for json met project codes
        {
            [JsonProperty("ProjectCodes")]
            public List<ProjectItem> allProjects { get; set; }

            [JsonProperty("ProjectTypes")]
            public List<string> projectTypes { get; set; }
        }

        public class ShortableProjectType : IShortNameable
        {
            public string TypeName { get; set; } // Een string member om de project type naam op te slaan

            public override string ToString() => TypeName;// Lange naam voor checkboxes
            public string ToStringShort() => TypeName.Substring(0, 3); // Korte naam voor textbox (kan eventueel aangepast worden)
                                                                       // Note: lengte strings moet minstens 3 zijn.
        }

        //private
        public ProjectData projectDataJson;
        List<ShortableProjectType> shortableProjectTypes;

        // Example of how you might read from a file in a WinForms application
        public void ReadProjectDataFromFile(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                projectDataJson = JsonConvert.DeserializeObject<ProjectData>(jsonString);

                if (projectDataJson == null)
                {
                    MessageBox.Show($"Fout bij het deserialiseren van JSON uit bestand: {filePath}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }

            }
            catch (FileNotFoundException)
            {
                MessageBox.Show($"Bestand niet gevonden: {filePath}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Fout bij het deserialiseren van JSON uit bestand: {filePath} - {ex.Message}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Er is een fatale fout opgetreden bij het lezen van het bestand: {filePath} - {ex.Message}", "Fatale Fout", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Environment.Exit(1);
            }

            shortableProjectTypes = projectDataJson.projectTypes.ConvertAll(type => new ShortableProjectType { TypeName = type });
        }

        public PanelUren()
        {
            InitializeComponents();

            string path = Path.Combine(Application.StartupPath, @"..\..\..", "ProjectCodes.json");
            path = Path.GetFullPath(path);  // normalized path without \..\
            ReadProjectDataFromFile(path);
            foreach (var project in projectDataJson.allProjects)
            {
                project.projectTypeInt = project.projectCode.GetHashCode();
            }
        }

        private void InitializeComponents()
        {
            this.Dock = DockStyle.Fill;
            this.BorderStyle = BorderStyle.FixedSingle;

            dv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                RowCount = 21,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,

            };

            AddColumns();
            this.Controls.Add(dv);
            this.BackColor = Draw.Color.Magenta;

            // 1.CellFormatting        Wanneer de cel wordt getekend (lezen/weergeven)
            // 2.CellBeginEdit         Gebruiker begint met editen
            // 3.EditingControlShowing De bewerkbare control (bijv. TextBox, ComboBox) wordt getoond
            // 4.CellValidating        Net voordat de bewerking wordt afgesloten (valideer invoer)
            // 5.CellEndEdit           Bewerken is klaar, waarde is toegepast

            dv.CellClick += dv_CellClick;
            dv.SelectionChanged += dv_SelectionChanged;
            dv.CellFormatting += dv_CellFormatting;
            //dv.CellBeginEdit += dv_CellBeginEdit;
            dv.EditingControlShowing += dv_EditingControlShowing;
            dv.CellValidating += dv_CellValidating;
            dv.CellEndEdit += dv_CellEndEdit;

            int row, col;
            bool b = true;
            for (row = 0; row < rowTotal2; ++row)
            {
                if (row == rowTotal1) continue;
                if (b)
                    dv.Rows[row].DefaultCellStyle.BackColor = Draw.Color.LightGoldenrodYellow;
                b = !b;
            }

            // Maak vlak links-onder grijs
            // Conflicteert met dv.Rows[row].DefaultCellStyle.BackColor
            // for (row = rowSpacer + 1; row < rowTotal2; ++row)
            // {
            //     for (col = 0; col < colKm - 1; ++col)
            //         dv.Rows[row++].Cells[col].Style.BackColor = Draw.Color.DarkGray;
            // 
            // }

            // RR! Het is wellicht beter om dit uit het template te lezen.
            row = rowSpacer + 1;
            col = colKm - 1;
            dv.Rows[row++].Cells[col].Value = "Dokter/tandarts";
            dv.Rows[row++].Cells[col].Value = "Buitengewoon verlof";
            dv.Rows[row++].Cells[col].Value = "Feestdagen";
            dv.Rows[row++].Cells[col].Value = "Tijd voor tijd";
            dv.Rows[row++].Cells[col].Value = "Vakantieverlof";
            dv.Rows[row++].Cells[col].Value = "Ziek";

            // RR! Het is wellicht beter om dit uit het template te lezen.
            row = rowSpacer + 1;
            col++;
            dv.Rows[row++].Cells[col].Value = 1;
            dv.Rows[row++].Cells[col].Value = 2;
            dv.Rows[row++].Cells[col].Value = 3;
            dv.Rows[row++].Cells[col].Value = 4;
            dv.Rows[row++].Cells[col].Value = 5;
            dv.Rows[row++].Cells[col].Value = 6;

            dv.Rows[rowSpacer].DefaultCellStyle.BackColor = Draw.Color.DarkGray;
            //dv.Rows[rowSpacer].DefaultCellStyle.ForeColor = Draw.Color.DarkGray;
            dv.Rows[rowSpacer].ReadOnly = true;

            dv.Rows[rowTotal1].ReadOnly = true;
            dv.Rows[rowTotal2].ReadOnly = true;
            dv.Rows[rowTotal1].DefaultCellStyle.Font = new Draw.Font(dv.Font, FontStyle.Bold);
            dv.Rows[rowTotal2].DefaultCellStyle.Font = new Draw.Font(dv.Font, FontStyle.Bold);
            dv.Rows[rowTotal1].Cells[colKm - 1].Value = "Totaal";
            dv.Rows[rowTotal2].Cells[colKm - 1].Value = "Totaal";

            dv.Columns[colTotal].ReadOnly = true;
            dv.Columns[colTotal].DefaultCellStyle.Font = new Draw.Font(dv.Font, FontStyle.Bold);

            for (row = rowTotal1; row <= rowTotal2; ++row)
            {
                for (col = 0; col <= colKm; ++col)
                    dv.Rows[row].Cells[col].ReadOnly = true;
            }

            for (col = colKm; col <= colTotal; ++col)
            {
                dv.Columns[col].Width = (col == colTotal) ? 50 : 40;
                //dv.Columns[col].MinimumWidth = 50,
                dv.Columns[col].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dv.Columns[col].Resizable = DataGridViewTriState.False;
            }
            dv.Columns[colKm].Width = 75;
            dv.Columns["Projectnummer"].MinimumWidth = 120;

            dv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            int defaultRowHeight = dv.Rows[0].Height;
            // Stel de standaard hoogte in voor alle rijen
            dv.RowTemplate.Height = defaultRowHeight;
            dv.Rows[rowSpacer].Height = defaultRowHeight / 2;

            dv.AllowUserToOrderColumns = false;
            dv.ColumnHeadersDefaultCellStyle.Font = new Draw.Font(dv.Font, FontStyle.Bold);

            dv.DefaultCellStyle.SelectionBackColor = Draw.Color.LightSkyBlue;
            dv.MultiSelect = false;
            dv.SelectionMode = DataGridViewSelectionMode.CellSelect;  // Optioneel: zet de selectie op celniveau
        }

        private void AddColumns()
        {
            // Maak kolom 0 onzichtbaar:
            dv.Columns[0].Visible = false;

            // Klantnaam
            dv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Klantnaam", HeaderText = "Klantnaam" });

            // Projecttype (CheckedComboBox filter)
            /*
            var columnProjectType = new DataGridViewColumn
            {
                Name = "Projecttype",
                HeaderText = "Projecttype",
                CellTemplate = new DataGridViewCheckedComboBoxCell()
            };
            */
            var columnProjectType = new DataGridViewCheckedComboBoxColumn
            {
                Name = "Projecttype",
                HeaderText = "Projecttype"
            };
            dv.Columns.Add(columnProjectType);

            // Projectnummer (FilteredComboBox)
            var columnProjectNumber = new DataGridViewColumn
            {
                Name = "Projectnummer",
                HeaderText = "Projectnummer",
                CellTemplate = new DataGridViewFilteredComboBoxCell()
            };
            dv.Columns.Add(columnProjectNumber);

            // km dienstreis
            colKm = dv.ColumnCount;
            dv.Columns.Add(new DataGridViewTextBoxColumn { Name = "kmDienstreis", HeaderText = "km dienstreis" });

            // Dag columns
            colMa = dv.ColumnCount;
            string[] days = { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
            foreach (var day in days)
            {
                dv.Columns.Add(new DataGridViewTextBoxColumn { Name = day, HeaderText = day });
            }

            // Totaal
            colTotal = dv.ColumnCount;
            dv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Totaal", HeaderText = "Totaal", ReadOnly = true });
        }

        // copy/submit methods
        private void CopyColumnProjectUren(string headerText, IXLWorksheet worksheet, DataGridView dv)
        {
            // Header met Klantnaam, projectcode, ma-zo, etc.
            // kopieer kolom met headerText:
            int colDataGrid = dv.GetColumnIndexByHeader(headerText);
            int colExcel = FormUren.FindColumn(worksheet, headerText, rowHeaderInTemplate);
            if (colDataGrid != -1 && colExcel != -1)
            {
                for (int row = 0; row < rowTotal1; ++row)
                {
                    FormUren.Assign(worksheet.Cell(row + rowHeaderInTemplate + 1, colExcel), dv.Rows[row].Cells[colDataGrid].Value);
                }
            }
        }

        public void CopyOnSubmit(IXLWorksheet worksheet)
        {
            var dv = this.dv;

            //const int rowTotal1 = 10;

            CopyColumnProjectUren("Klantnaam", worksheet, dv);
            CopyColumnProjectUren("km", worksheet, dv);
            var dagen = new[] { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
            foreach (var dag in dagen)
            {
                CopyColumnProjectUren(dag, worksheet, dv);
            }

        }


        // ComputeTotal1 (part of horizontal row of totals) for column with headerText
        public void ComputeTotal1(string headerText, IXLWorksheet worksheet)
        {
            int colDataGrid = dv.GetColumnIndexByHeader(headerText);
            if (colDataGrid != -1)
            {
                double sum = 0;
                for (int row = 0; row < rowTotal1; ++row)
                {
                    var value = dv.Rows[row].Cells[colDataGrid].Value.ToString();
                    if (double.TryParse(value, out double number))
                    {
                        sum += number;
                    }
                }
                //dv.Rows[rowTotal1].Cells[colDataGrid].Value = sum; // row met totalen (horizontaal) 
            }
        }

        private void ComputeTotalColumn(int col)
        {
            if (col < colKm)
                return;

            double sum = 0.0;
            for (int row = 0; row < rowTotal1; ++row)
            {
                var value = dv.Rows[row].Cells[col].Value?.ToString();
                if (double.TryParse(value, out double number))
                {
                    sum += number;
                }
            }

            if (sum < 0.499)
                dv.Rows[rowTotal1].Cells[col].Value = "";
            else
                dv.Rows[rowTotal1].Cells[col].Value = sum;

            //=======================================================

            if (col <= colKm)
                return;
            sum = 0.0;
            for (int row = rowTotal1; row < rowTotal2; ++row)
            {
                var value = dv.Rows[row].Cells[col].Value?.ToString();
                if (double.TryParse(value, out double number))
                {
                    sum += number;
                }
            }

            if (sum < 0.499)
                dv.Rows[rowTotal2].Cells[col].Value = "";
            else
                dv.Rows[rowTotal2].Cells[col].Value = sum;
        }

        private void ComputeTotalRow(int row)
        {
            if (row == rowTotal1 || row == rowSpacer || row == rowTotal2)
                return;

            double sum = 0.0;
            for (int col = colMa; col < colTotal; ++col)
            {
                var value = dv.Rows[row].Cells[col].Value?.ToString();
                if (double.TryParse(value, out double number))
                {
                    sum += number;
                }
            }

            if (sum < 0.499)
                dv.Rows[row].Cells[colTotal].Value = "";
            else
                dv.Rows[row].Cells[colTotal].Value = sum;
        }


        private void KmTextBox_KeyPress(object sender, KeyPressEventArgs ev)
        {
            if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar))
            {
                ev.Handled = true;
            }
        }

        private void TextBox_KeyPress_DayColumns(object sender, KeyPressEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar))
            {
                return;
            }

            if (e.KeyChar == '.' || e.KeyChar == ',')
            {
                if (textBox.Text.EndsWith(','))
                {
                    e.Handled = true;
                    return;
                }

                if (textBox.SelectionStart == textBox.Text.Length || textBox.Text.Length == 0)
                {
                    if (e.KeyChar == '.')
                    {
                        e.KeyChar = ',';
                    }
                    return;
                }
                else
                {
                    e.Handled = true;
                    return;
                }
            }

            e.Handled = true;
        }

        #region dv events

        private void ShowValidationError(string message, int rowIndex, string columnName)
        {
            MessageBox.Show(
                text: $"{message}\nRij: {rowIndex + 1}, Kolom: {columnName}",
                caption: "Validatiefout",
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error
            );
            dv.Rows[rowIndex].Cells[columnName].Value = null; // Reset de waarde
            dv.CurrentCell = dv.Rows[rowIndex].Cells[columnName]; // Zet de focus terug op de cel
        }

        private void dv_SelectionChanged(object sender, EventArgs e)
        {
            if (dv.SelectedCells.Count > 0)
            {
                var selectedCell = dv.SelectedCells[0];

                // Dit zorgt ervoor dat ReadOnly cellen
                // zowel niet bewerkbaar als niet selecteerbaar zijn.
                if (selectedCell.ReadOnly)
                {
                    dv.ClearSelection();
                }

                // Als er meerdere cellen geselecteerd zijn,
                // deselecteer dan alles behalve de eerste geselecteerde cel
                if (dv.SelectedCells.Count > 1)
                {
                    for (int i = 1; i < dv.SelectedCells.Count; i++)
                    {
                        dv.SelectedCells[i].Selected = false;
                    }
                }
            }
        }

        // Dit zorgt ervoor dat ReadOnly cellen
        // zowel niet bewerkbaar als niet selecteerbaar zijn.
        private void dv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.IsInvalidCell()) return;
            // Controleer of de geselecteerde cel niet bewerkbaar is
            if (dv.Rows[e.RowIndex].Cells[e.ColumnIndex].ReadOnly)
            {
                dv.ClearSelection();
            }
        }

        private void dv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.IsInvalidCell()) return;

            string columnName = dv.Columns[e.ColumnIndex].Name;

            if (columnName == "Ma" || columnName == "Di" || columnName == "Wo" ||
                columnName == "Do" || columnName == "Vr" || columnName == "Za" || columnName == "Zo")
            {
                if (e.Value != null && e.Value is string value && value.EndsWith(","))
                {
                    e.Value = value + "5";
                    e.FormattingApplied = true;
                }
            }
        }

        /*
        private void dv_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.IsInvalidCell()) return;
            int row = e.RowIndex;
            int col = e.ColumnIndex;
        }
        */

        private void dv_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dv.CurrentCell == null) return;
            string columnName = dv.Columns[dv.CurrentCell.ColumnIndex].Name;
            editControl = e.Control;

            if (columnName == "Projecttype" && e.Control is CheckedComboBox comboProjectType)
            {
                comboProjectType.SetDataSource(shortableProjectTypes);
                var cellValue = dv.Rows[dv.CurrentCell.RowIndex].Cells[dv.CurrentCell.ColumnIndex].Value; //RR! Hack
                if (cellValue is string combinedValue && !string.IsNullOrEmpty(combinedValue))
                {
                    string[] parts = combinedValue.Split(';');
                    if (parts.Length == 2)
                    {
                        //string shortNames = parts[0];
                        string longNames = parts[1];
                        comboProjectType.SetCheckedItems(longNames.Split(',').ToList());

                        //RR!! set flag monitoring
                    }
                }
            }
            else if (columnName == "Projectnummer" && e.Control is FilteredComboBox<ProjectItem> comboProjectCode)
            {
                var editingControl = dv.EditingControl as CheckedComboBox;

                if (editingControl != null)
                {
                    var selectedTypes = editingControl.GetCheckedValues();
                    var selectedTypeInts = selectedTypes
                        .Select(type => type.GetHashCode())
                        .ToList();

                    comboProjectCode.DataSource = projectDataJson.allProjects
                        .Where(project => selectedTypeInts.Contains(project.projectTypeInt))
                        .ToList();
                }
                else
                {
                    comboProjectCode.DataSource = projectDataJson.allProjects.ToList();
                }

                comboProjectCode.DisplayMember = "ToString";
                comboProjectCode.ValueMember = "ProjectCode";
            }

            if ((columnName == "Ma" || columnName == "Di" || columnName == "Wo" ||
                 columnName == "Do" || columnName == "Vr" || columnName == "Za" || columnName == "Zo")
                && e.Control is TextBox dayTextBox)
            {
                dayTextBox.KeyPress += (s, ev) =>
                {
                    dayTextBox.KeyPress -= TextBox_KeyPress_DayColumns;
                    dayTextBox.KeyPress += TextBox_KeyPress_DayColumns;
                };
            }

            if (columnName == "kmDienstreis" && e.Control is TextBox kmTextBox)
            {
                kmTextBox.KeyPress -= KmTextBox_KeyPress;
                kmTextBox.KeyPress += KmTextBox_KeyPress;
            }
        }

        private void dv_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.IsInvalidCell()) return;
            string columnName = dv.Columns[e.ColumnIndex].Name;

            if (columnName == "kmDienstreis" && !string.IsNullOrEmpty(e.FormattedValue?.ToString()))
            {
                if (int.TryParse(e.FormattedValue.ToString(), out int km))
                {
                    if (km >= 1000)
                    {
                        e.Cancel = true;
                        MessageBox.Show("Het aantal kilometers mag niet hoger zijn dan 999.");
                    }
                }
                else
                {
                    e.Cancel = true;
                    MessageBox.Show("Voer alleen hele getallen in voor km dienstreis.");
                }
            }
        }

        private void dv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.IsInvalidCell()) return;

            string columnName = dv.Columns[e.ColumnIndex].Name;
            DataGridViewCell cell = dv.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string value = cell.Value?.ToString();

            switch (columnName)
            {
                case "Projecttype":
                    if (dv.EditingControl is CheckedComboBox combo1)
                    {
                        if (combo1.IsPanelVisible)
                        {
                            return; // Verlaat de event handler als het dropdown panel nog zichtbaar is
                        }
                        cell.Value = ((DataGridViewCheckedComboBoxCell)cell).GetCombinedValue(combo1);
                    }
                    else if (editControl is CheckedComboBox combo2)
                    {
                        if (combo2.IsPanelVisible)
                        {
                            return; // Verlaat de event handler als het dropdown panel nog zichtbaar is
                        }
                        cell.Value = combo2.GetCombinedValue();
                    }
                    else
                    {
                        // Debug.Assert(false);
                        cell.Value = "Leeg"; // string.Empty;
                    }

                    break;

                case "Ma":
                case "Di":
                case "Wo":
                case "Do":
                case "Vr":
                case "Za":
                case "Zo":
                    if (!string.IsNullOrEmpty(value))
                    {
                        string valueToParse = value.EndsWith(",") ? value + "5" : value;
                        valueToParse = valueToParse.Replace(',', '.');

                        if (double.TryParse(valueToParse, out double hours))
                        {
                            cell.Value = hours;
                            ComputeTotalRow(e.RowIndex);
                            ComputeTotalColumn(e.ColumnIndex);
                            ComputeTotalColumn(colTotal);
                        }
                        else
                        {
                            ShowValidationError($"Ongeldige invoer: '{value}'. Voer een getal in (bv. 1,5 of 2).", e.RowIndex, columnName);
                            cell.Value = value;
                        }
                    }
                    break;

                case "km Dienstreis":
                    ComputeTotalColumn(e.ColumnIndex);
                    break;
            }
        }
        #endregion dv events
    }
}// namespace CustomControls