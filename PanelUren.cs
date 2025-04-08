using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CustomControls;
using System.ComponentModel;

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
        private DataGridView dataGridView1;
        private List<ProjectItem> allProjects;

        public PanelUren(List<ProjectItem> projects)
        {
            this.allProjects = projects ?? throw new ArgumentNullException(nameof(projects));
            InitializeComponents();
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
        }

        private void AddColumns()
        {
            // Klantnaam
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Klantnaam", HeaderText = "Klantnaam" });

            // Projecttype (CheckedComboBox filter)
            var typeColumn = new DataGridViewColumn
            {
                Name = "Projecttype",
                HeaderText = "Projecttype",
                CellTemplate = new DataGridViewCheckedComboBoxCell()
            };
            dataGridView1.Columns.Add(typeColumn);

            // Projectnummer (FilteredComboBox)
            var nummerColumn = new DataGridViewColumn
            {
                Name = "Projectnummer",
                HeaderText = "Projectnummer",
                CellTemplate = new DataGridViewFilteredComboBoxCell()
            };
            dataGridView1.Columns.Add(nummerColumn);

            // km dienstreis
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "kmDienstreis", HeaderText = "km dienstreis" });

            // Dag columns
            string[] days = { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
            foreach (var day in days)
            {
                dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = day, HeaderText = day });
            }

            // Totaal
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Totaal", HeaderText = "Totaal", ReadOnly = true });
        }

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Als Projecttype verandert, reset Projectnummer
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Projecttype")
            {
                dataGridView1.Rows[e.RowIndex].Cells["Projectnummer"].Value = null;
            }
        }

        private void DataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            string columnName = dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].Name;

            if (columnName == "Projecttype" && e.Control is CheckedComboBox typeCombo)
            {
                // Configureer Projecttype CheckedComboBox
                typeCombo.DataSource = allProjects
                    .Select(p => p.ProjectTypeInt)
                    .Distinct()
                    .OrderBy(i => i)
                    .ToList();

                typeCombo.ValueSeparator = ",";
            }
            else if (columnName == "Projectnummer" && e.Control is FilteredComboBox<ProjectItem> nummerCombo)
            {
                // Haal geselecteerde types op
                var typeCell = dataGridView1.CurrentRow.Cells["Projecttype"];
                var selectedTypes = typeCell.Value?.ToString()?
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList() ?? new List<int>();

                // Filter projecten op geselecteerde types
                nummerCombo.DataSource = allProjects
                    .Where(p => selectedTypes.Contains(p.ProjectTypeInt))
                    .OrderBy(p => p.ProjectCode)
                    .ToList();

                nummerCombo.DisplayMember = "ToString";
                nummerCombo.ValueMember = "ProjectCode";
            }

        // Validatie voor dagen en km
        if ((columnName == "Ma" || columnName == "Di" || columnName == "Wo" || 
             columnName == "Do" || columnName == "Vr" || columnName == "Za" || columnName == "Zo") 
            && e.Control is TextBox dayTextBox)
        {
            dayTextBox.KeyPress += (s, ev) =>
            {
                if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar) && ev.KeyChar != '.')
                {
                    ev.Handled = true;
                    return;
                }
                
                if (ev.KeyChar == '.' && dayTextBox.Text.Contains("."))
                {
                    ev.Handled = true;
                    return;
                }
                
                if (dayTextBox.Text.Contains(".") && 
                    dayTextBox.SelectionStart > dayTextBox.Text.IndexOf('.'))
                {
                    ev.Handled = true;
                }
            };
        }
        
        if (columnName == "kmDienstreis" && e.Control is TextBox kmTextBox)
        {
            kmTextBox.KeyPress += (s, ev) =>
            {
                if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar))
                {
                    ev.Handled = true;
                }
            };
        }
    }
    
    private void DataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
    {
        string columnName = dataGridView1.Columns[e.ColumnIndex].Name;
        
        // Validate km dienstreis
        if (columnName == "kmDienstreis" && !string.IsNullOrEmpty(e.FormattedValue?.ToString()))
        {
            if (!int.TryParse(e.FormattedValue.ToString(), out _))
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

    // ProjectItem class met alle vereiste properties
    public class ProjectItem : IShortNameable
    {
        public int ProjectCode { get; set; }
        public string ProjectType { get; set; }
        public int ProjectTypeInt { get; set; } // Hash of unieke ID voor het type
        public string Description { get; set; }

        public string ToString() => $"{ProjectCode} - {Description}";
        public string ToStringShort() => $"{ProjectCode}";
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
                combo.ValueSeparator = ",";
                if (initialFormattedValue != null)
                {
                    combo.SetItemsChecked(initialFormattedValue.ToString().Split(','));
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

            if (DataGridView.EditingControl is FilteredComboBox<ProjectItem> combo)
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

        public override Type EditType => typeof(FilteredComboBox<ProjectItem>);
        public override Type ValueType => typeof(int);
        public override object DefaultNewRowValue => null;
    }
}// namespace CustomControls