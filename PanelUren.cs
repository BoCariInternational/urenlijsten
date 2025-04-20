using System;
using System.ComponentModel;
using System.Diagnostics;
using Draw = System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections.Generic;
using ClosedXML.Excel;
//using DocumentFormat.OpenXml.Spreadsheet;
using Newtonsoft.Json;
using CustomControls;
using static Urenlijsten_App.PanelUren;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Runtime.CompilerServices;


// Vereiste interfaces
public interface IShortNameable
{
    //string ToString();      // Lange naam voor checkboxes
    public interface IShortNameable
    {
        // Override ToString geeft lange representatie.
        string ShortString { get; }  // Property voor korte string
        ProjectItem Item { get; }    // Property voor het verkrijgen van het item
    }
}
public static class MyExtensions
{
    /// <summary>
    /// Converteert een DateTime naar een string in Nederlands formaat (dd/MM/yyyy).
    /// </summary>
    public static string ToStringNL(this DateTime date)
    {
        return date.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Converteert een nullable DateTime naar een string in Nederlands formaat (dd/MM/yyyy) of geeft een lege string terug bij null.
    /// </summary>
    public static string ToStringNL(this DateTime? date)
    {
        return date?.ToStringNL() ?? string.Empty;
    }

    public static bool TryParseDoubleExt(this string value, out double result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = 0; // Je kunt hier een andere default waarde kiezen als dat nodig is
            return false;
        }

        return double.TryParse(
            value.Replace(',', '.'),
            NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out result
        );
    }

    //===

    public static double? DoubleFromObj(this object obj)
    {
        // obj is vaak een object uit DataGridView cell
        if (obj == null)
        {
            return null;
        }

        if (obj is double d)
        {
            return d;
        }

        if (obj is float f)
        {
            return (double)f;
        }

        if (obj is int i)
        {
            return (double)i;
        }

        if (obj is decimal dec)
        {
            return (double)dec;
        }

        if (obj is string s)
        {
            if (s.Replace(',', '.').TryParseDoubleExt(out double result))
            {
                return result;
            }
        }

        return null; // Kon niet naar double converteren
    }
}

namespace Urenlijsten_App
{
    public class PanelUren : Panel
    {
        public DataGridView dv;
        //private DataGridViewCheckedComboBoxColumn columnProjectType = null; //RR!
        // In dv:
        private int colMa;
        private int colTotal;
        private int colKm;
        public const int rowTotal1 = 10;
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

            public override string ToString()
            {
                return $"{projectCode} - {description}";
            }

            public string ShortString => $"{projectCode}";

            // De Item property die vereist is volgens de interface
            public ProjectItem Item => this;
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

            FilteredComboBox<ProjectItem>._projectItems = projectDataJson.allProjects;
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
            dv.KeyDown += dv_KeyDown;

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
            dv.Columns["Urencode"].MinimumWidth = 120;

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

            // Hide last two rows before final total
            dv.Rows[rowTotal2 - 2].Visible = false;
            dv.Rows[rowTotal2 - 1].Visible = false;
        }

        private void AddColumns()
        {
            // Maak kolom 0 onzichtbaar:
            dv.Columns[0].Visible = false;

            // Klantnaam
            dv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Klantnaam",
                HeaderText = "Klantnaam"
            });

            dv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Projectnummer",
                HeaderText = "Projectnummer"
            });

            // Projecttype (CheckedComboBox filter)

            //var columnProjectType = new DataGridViewCheckedComboBoxColumn
            //{
            //    Name = "Projecttype",
            //    HeaderText = "Projecttype",
            //    Visible = false, // RR!! Temporarily hide the column
            //};
            //dv.Columns.Add(columnProjectType);

            // Urencode (FilteredComboBox)
            var columnProjectNumber = new DataGridViewComboBoxColumn
            {
                Name = "Urencode",
                HeaderText = "Urencode",
                CellTemplate = new FilteredComboBoxCell<ProjectItem>(),

                /* Een DataGridViewCell zelf heeft geen directe DisplayMember en ValueMember
                 * eigenschappen zoals een ComboBox of een ListControl.
                 * In een DataGridView kun je echter een kolom van het type DataGridViewComboBoxColumn
                 * gebruiken, en deze kolom heeft wel de eigenschappen DisplayMember en ValueMember. 
                 * Deze eigenschappen bepalen hoe de gegevens in de cel worden weergegeven en welke waarde
                 * wordt opgeslagen.
                 */ 
            };
            dv.Columns.Add(columnProjectNumber);

            dv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Omschrijving",
                HeaderText = "Omschrijving",
                ReadOnly = true
            });

            // km dienstreis
            colKm = dv.ColumnCount;
            dv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "kmDienstreis",
                HeaderText = "km dienstreis"
            });

            // Dag columns
            colMa = dv.ColumnCount;
            string[] days = { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
            foreach (var day in days)
            {
                dv.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = day,
                    HeaderText = day
                });
            }

            // Totaal
            colTotal = dv.ColumnCount;
            dv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Totaal",
                HeaderText = "Totaal",
                ReadOnly = true
            });
        }

        // copy/submit methods
        private void ColumnToExcel(string headerText, IXLWorksheet worksheet, DataGridView dv, bool numeric = false)
        {
            // Header met Klantnaam, projectcode, ma-zo, etc.
            // kopieer kolom met headerText:
            int colDataGrid = dv.GetColumnIndexByHeader(headerText);
            int colExcel = FormUren.FindColumn(worksheet, headerText, rowHeaderInTemplate);
            if (colDataGrid != -1 && colExcel != -1)
            {
                for (int row = 0; row < rowTotal1; ++row)
                {
                    object cellValue = dv.Rows[row].Cells[colDataGrid].Value;
                    double? d = cellValue.DoubleFromObj();
                    if (d.HasValue)
                    {
                        FormUren.Assign(worksheet.Cell(row + rowHeaderInTemplate + 1, colExcel), d.Value);
                    }
                    else
                    {
                        FormUren.Assign(worksheet.Cell(row + rowHeaderInTemplate + 1, colExcel), d);

                    }
                }
            }
        }

        public void OnSubmit(IXLWorksheet worksheet)
        {
            //RR!! uren, omschrijving
            ColumnToExcel("Klantnaam", worksheet, dv);
            ColumnToExcel("Projectnummer", worksheet, dv);
            ColumnToExcel("Urencode", worksheet, dv);
            ColumnToExcel("Omschrijving", worksheet, dv);
            ColumnToExcel("km", worksheet, dv, true);

            var dagen = new[] { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
            foreach (var dag in dagen)
            {
                ColumnToExcel(dag, worksheet, dv, true);
            }
        }

        private void ComputeTotals(int row, int col)
        {
            if (col == colKm)
            {
                ComputeTotalColumn(col);
            }
            else if (col >= colMa)
            {
                ComputeTotalRow(row);
                ComputeTotalColumn(col);
                ComputeTotalColumn(colTotal);
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

        private void dv_KeyDown(object sender, KeyEventArgs e)
        {
            // Controleer of de Delete-toets is ingedrukt en of er een cel is geselecteerd
            if (e.KeyCode == Keys.Delete && dv.CurrentCell != null)
            {
                DataGridViewCell selectedCell = dv.CurrentCell;
                selectedCell.Value = "";
                ComputeTotals(selectedCell.RowIndex, selectedCell.ColumnIndex);
            }
        }

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
            //!!! At this point, the editing control is not complete, so for instance,
            // you can't tell a combobox to show it's dropdown...
            // Really, MS did think this through a lot.
            if (dv.CurrentCell == null) 
                return;
            try
            {
                string columnName = dv.Columns[dv.CurrentCell.ColumnIndex].Name;
                DataGridViewCell cell = dv.Rows[dv.CurrentCell.RowIndex].Cells[dv.CurrentCell.ColumnIndex];

                switch (columnName)
                {
                    case "Projecttype":
                        if (e.Control is CheckedComboBox comboProjectType)
                        {
                            if (cell.Value is string combinedValue && !string.IsNullOrEmpty(combinedValue))
                            {
                                string[] parts = combinedValue.Split(';');
                                if (parts.Length == 2)
                                {
                                    string longNames = parts[1];
                                    comboProjectType.SetCheckedItems(longNames.Split(',').ToList());
                                    // RR!! set flag monitoring
                                }
                            }
                        }
                        break;

                    case "Urencode":
                        if (e.Control is FilteredComboBox<ProjectItem> comboProjectCode)
                        {
                            ProjectItem item = cell?.Value as ProjectItem;
                            comboProjectCode.SelectedItem = item;
                            string text = item?.ToString() ?? string.Empty;
                            comboProjectCode.ApplyFilter(text, true);
                        }
                        break;

                    case "Ma":
                    case "Di":
                    case "Wo":
                    case "Do":
                    case "Vr":
                    case "Za":
                    case "Zo":
                        if (e.Control is TextBox dayTextBox)
                        {
                            dayTextBox.KeyPress -= TextBox_KeyPress_DayColumns;
                            dayTextBox.KeyPress += TextBox_KeyPress_DayColumns;
                        }
                        break;

                    case "kmDienstreis":
                        if (e.Control is TextBox kmTextBox)
                        {
                            kmTextBox.KeyPress -= KmTextBox_KeyPress;
                            kmTextBox.KeyPress += KmTextBox_KeyPress;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ;
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
            //!!!  At this point, the edit control is already gone...
            // Really, MS did think this through a lot.
            // Gebruik DataGridView.CellValidated:
            if (e.IsInvalidCell()) return;
            try
            {
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
                            //RR!!cell.Value = ((DataGridViewCheckedComboBoxCell)cell).GetCombinedValue(combo1);
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
                    case "Urencode":
                        // At this point, the edit control is already gone...
                        cell.Value = null;
                        if (dv.EditingControl is FilteredComboBox<ProjectItem> comboProjectCode)
                        {
                            if (comboProjectCode.SelectedIndex >= 0)
                            {
                                ProjectItem item = comboProjectCode.SelectedItem as ProjectItem;
                                cell.Value = item;
                            }
                        }
                        break;
                    case "Ma":
                    case "Di":
                    case "Wo":
                    case "Do":
                    case "Vr":
                    case "Za":
                    case "Zo":
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            cell.Value = "";
                            ComputeTotals(e.RowIndex, e.ColumnIndex);
                        }
                        else
                        {
                            string valueToParse = value.EndsWith(",") ? value + "5" : value;
                            if (valueToParse.TryParseDoubleExt(out double hours))
                            {
                                cell.Value = hours;
                                ComputeTotals(e.RowIndex, e.ColumnIndex);
                            }
                            else
                            {
                                ShowValidationError($"Ongeldige invoer: '{value}'. Voer een getal in (bv. 1,5 of 2).", e.RowIndex, columnName);
                                cell.Value = value;
                            }
                        }
                        break;

                    case "kmDienstreis":
                        ComputeTotalColumn(e.ColumnIndex);
                        break;
                }
            }
            catch (Exception ex)
            {
                ;
            }
        }
        #endregion dv events
    }
}// namespace Urenlijsten_App