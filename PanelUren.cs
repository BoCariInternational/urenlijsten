using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CustomControls;
using System.ComponentModel;
using Newtonsoft.Json;


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
        public DataGridView dataGridView1;

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
            public string ToStringShort() =>  TypeName.Substring(0, 3); // Korte naam voor textbox (kan eventueel aangepast worden)
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

            dataGridView1 = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                RowCount = 20,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };

            AddColumns();
            this.Controls.Add(dataGridView1);

            dataGridView1.CellValidating += DataGridView1_CellValidating;
            dataGridView1.EditingControlShowing += DataGridView1_EditingControlShowing;
            dataGridView1.CellEndEdit += DataGridView1_CellEndEdit;
            dataGridView1.CellFormatting += DataGridView1_CellFormatting;
        }

        private void AddColumns()
        {
            // Maak kolom 0 onzichtbaar:
            dataGridView1.Columns[0].Visible = false;

            // Klantnaam
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Klantnaam", HeaderText = "Klantnaam" });

            // Projecttype (CheckedComboBox filter)
            var columnProjectType = new DataGridViewColumn
            {
                Name = "Projecttype",
                HeaderText = "Projecttype",
                CellTemplate = new DataGridViewCheckedComboBoxCell()
            };
            dataGridView1.Columns.Add(columnProjectType);

            // Projectnummer (FilteredComboBox)
            var columnProjectNumber = new DataGridViewColumn
            {
                Name = "Projectnummer",
                HeaderText = "Projectnummer",
                CellTemplate = new DataGridViewFilteredComboBoxCell()
            };
            dataGridView1.Columns.Add(columnProjectNumber);

            // km dienstreis
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "kmDienstreis", HeaderText = "km dienstreis" });
            int count = dataGridView1.ColumnCount;

            // Dag columns
            string[] days = { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
            foreach (var day in days)
            {
                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = day, HeaderText = day });
            }

            // Totaal
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Totaal", HeaderText = "Totaal", ReadOnly = true });
            count = dataGridView1.ColumnCount;
        }

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                string columnName = dataGridView1.Columns[e.ColumnIndex].Name;

                // Als Projecttype verandert, reset Projectnummer
                if (columnName == "Projecttype")
                {
                    dataGridView1.Rows[e.RowIndex].Cells["Projectnummer"].Value = null;
                }
                //else

                if (columnName == "Ma" || columnName == "Di" || columnName == "Wo" ||
                    columnName == "Do" || columnName == "Vr" || columnName == "Za" || columnName == "Zo")
                {
                    DataGridViewCell cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
                    string value = cell.Value?.ToString();

                    if (!string.IsNullOrEmpty(value))
                    {
                        string valueToParse = value;
                        if (value.EndsWith(","))
                        {
                            valueToParse += "5";
                        }

                        // Vervang komma door punt voor het parsen
                        valueToParse = valueToParse.Replace(',', '.');
                        if (double.TryParse(valueToParse, out double hours))
                        {
                            cell.Value = hours; // Sla de geparste double waarde op
                        }
                        else
                        {
                            MessageBox.Show($"Ongeldige invoer: '{value}'. Voer een getal in (bv. 1,5 of 2). \nRij: {e.RowIndex}, Kolom: {columnName}", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

            }
        }

        private void DataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            string columnName = dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].Name;

            if (columnName == "Projecttype" && e.Control is CheckedComboBox comboProjectType)
            {
                comboProjectType.SetDataSource(shortableProjectTypes);
            }
            else if (columnName == "Projectnummer" && e.Control is FilteredComboBox<ProjectItem> comboProjectCode)
            {
                // Haal de CheckedComboBox van de kolom "Projecttype" op
                var editingControl = dataGridView1.EditingControl as CheckedComboBox;

                if (editingControl != null)
                {
                    // Stap 1: Haal de geselecteerde items op als strings (TypeName van ProjectTypeWrapper)
                    var selectedTypes = editingControl.GetCheckedValues();

                    // Stap 2: Converteer de geselecteerde strings naar hash codes
                    var selectedTypeInts = selectedTypes
                        .Select(type => type.GetHashCode())
                        .ToList();

                    // Stap 4: Filter projectDataJson.allProjects op basis van de hash codes
                    comboProjectCode.DataSource = projectDataJson.allProjects
                        .Where(project => selectedTypeInts.Contains(project.projectTypeInt))
                        //.OrderBy(p => p.projectCode)
                        .ToList();
                }
                else
                {
                    // Als er geen editing control is (bijv. bij het laden van de grid),
                    // toon dan alle projecten
                    comboProjectCode.DataSource = projectDataJson.allProjects
                        //.OrderBy(p => p.projectCode)
                        .ToList();
                }

                /* When you set the DataSource of a ComboBox to a collection of objects (like a List<ProjectItem>), 
                 * the ComboBox needs to know which property of those objects to display in the dropdown list and 
                 * which property to use as the underlying value when an item is selected. 
                 * This is where DisplayMember and ValueMember come in.
                 * 
                 * Explanation of the Properties:
                 * DisplayMember = "ToString";: This property tells the ComboBox which property of the objects in
                 * its DataSource should be used to display the text for each item in the dropdown list. In this 
                 * specific case, it's set to "ToString". This means that when the ComboBox needs to show the text
                 * for a ProjectItem object, it will call the ToString() method of that object and use the returned string. 
                 * Looking back at the ProjectItem class definition, the ToString() method is defined as 
                 * public string ToString() => $"{ProjectCode} - {Description}";. 
                 * So, each item in the dropdown will show the project code followed by a hyphen and the description.
                 * 
                 * ValueMember = "ProjectCode";: This property tells the ComboBox which property of the objects in its
                 * DataSource should be used as the actual value associated with each item. When an item is selected in
                 * the ComboBox, the SelectedValue property of the ComboBox will return the value of the property
                 * specified by ValueMember for the selected object. In this case, it's set to "ProjectCode". 
                 */

                comboProjectCode.DisplayMember = "ToString";
                comboProjectCode.ValueMember = "ProjectCode";
            }

            // Validatie voor dagen en km
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
                kmTextBox.KeyPress -= KmTextBox_KeyPress; // Ontkoppel eventueel bestaande handler
                kmTextBox.KeyPress += KmTextBox_KeyPress; // Koppel de named method handler
            }
        }// DataGridView1_EditingControlShowing

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

        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex >= 0)
            {
                string columnName = dataGridView1.Columns[e.ColumnIndex].Name;

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
        }

        private void DataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            string columnName = dataGridView1.Columns[e.ColumnIndex].Name;

            // Validate km dienstreis
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

            // Validate day columns
            if (columnName == "Ma" || columnName == "Di" || columnName == "Wo" ||
                columnName == "Do" || columnName == "Vr" || columnName == "Za" || columnName == "Zo")
            {
                if (!string.IsNullOrEmpty(e.FormattedValue?.ToString()))
                {
                    string value = e.FormattedValue.ToString();

                    if (value.Contains("."))
                    {
                        if (!value.EndsWith(".5") || !double.TryParse(value, out _))
                        {
                            e.Cancel = true;
                            MessageBox.Show("Alleen .5 is toegestaan voor halve uren");
                        }
                    }
                    else if (!int.TryParse(value, out _))
                    {
                        e.Cancel = true;
                        MessageBox.Show("Voer alleen cijfers in voor uren");
                    }
                }
            }
        }
    }

    // CheckedComboBox cel implementatie
    public class DataGridViewCheckedComboBoxCell : DataGridViewTextBoxCell
    {
        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
            DataGridViewCellStyle cellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, cellStyle);

            if (DataGridView.EditingControl is CheckedComboBox combo)
            {
                if (initialFormattedValue != null)
                {
                    //RR! combo.SetItemsChecked(initialFormattedValue.ToString().Split(','));
                }
            }
        }

        public override Type EditType => typeof(CheckedComboBox);
        public override Type ValueType => typeof(string);
        public override object DefaultNewRowValue => string.Empty;
    }

    // FilteredComboBox cel implementatie
    public class DataGridViewFilteredComboBoxCell : DataGridViewTextBoxCell
    {
        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
            DataGridViewCellStyle cellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, cellStyle);

            if (DataGridView.EditingControl is FilteredComboBox<PanelUren.ProjectItem> combo)
            {
                if (initialFormattedValue != null)
                {
                    combo.SelectedValue = initialFormattedValue;
                }
            }
        }

        protected override object GetFormattedValue(object value, int rowIndex,
            ref DataGridViewCellStyle cellStyle, TypeConverter valueTypeConverter,
            TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
        {
            return value?.ToString() ?? string.Empty;
        }

        public override Type EditType => typeof(FilteredComboBox<PanelUren.ProjectItem>);
        public override Type ValueType => typeof(int);
        public override object DefaultNewRowValue => null;
    }
}// namespace CustomControls