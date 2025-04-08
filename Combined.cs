using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using UserControls;

public class PanelUren : Panel
{
    private DataGridView dataGridView1;

    public PanelUren()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Panel setup
        this.Dock = DockStyle.Fill;
        this.BorderStyle = BorderStyle.FixedSingle;

        // Create and configure DataGridView
        dataGridView1 = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            RowCount = 20,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };

        // Add columns
        AddColumns();

        // Add DataGridView to panel
        this.Controls.Add(dataGridView1);

        // Set up events
        dataGridView1.CellValidating += DataGridView1_CellValidating;
        dataGridView1.EditingControlShowing += DataGridView1_EditingControlShowing;
    }

    private void AddColumns()
    {
        // Klantnaam column (string)
        dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Klantnaam",
            HeaderText = "Klantnaam"
        });

        // Projecttype column (CheckedComboBox)
        dataGridView1.Columns.Add(new DataGridViewColumn
        {
            Name = "Projecttype",
            HeaderText = "Projecttype",
            CellTemplate = new DataGridViewCheckedComboBoxCell()
        });

        // Projectnummer column (FilteredComboBox<ProjectCode>)
        dataGridView1.Columns.Add(new DataGridViewColumn
        {
            Name = "Projectnummer",
            HeaderText = "Projectnummer",
            CellTemplate = new DataGridViewFilteredComboBoxCell()
        });

        // km dienstreis column
        dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "kmDienstreis",
            HeaderText = "km dienstreis"
        });

        // Day columns
        string[] days = { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
        foreach (var day in days)
        {
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = day,
                HeaderText = day
            });
        }

        // Totaal column (computed)
        dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Totaal",
            HeaderText = "Totaal",
            ReadOnly = true
        });
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

    private void DataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
    {
        string columnName = dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].Name;

        // Handle day columns input restriction
        if ((columnName == "Ma" || columnName == "Di" || columnName == "Wo" ||
             columnName == "Do" || columnName == "Vr" || columnName == "Za" || columnName == "Zo")
            && e.Control is TextBox textBox)
        {
            textBox.KeyPress += (s, ev) =>
            {
                if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar) && ev.KeyChar != '.')
                {
                    ev.Handled = true;
                    return;
                }

                // Only allow one decimal point
                if (ev.KeyChar == '.' && textBox.Text.Contains("."))
                {
                    ev.Handled = true;
                    return;
                }

                // If decimal exists, don't allow more characters after it
                if (textBox.Text.Contains(".") &&
                    textBox.SelectionStart > textBox.Text.IndexOf('.'))
                {
                    ev.Handled = true;
                }
            };
        }

        // Handle km column numeric only
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
}

// ProjectCode class for the FilteredComboBox
public class ProjectCode
{
    public int Code { get; set; }
    public string Description { get; set; }

    public override string ToString() => $"{Code} - {Description}";
}

// Custom cell for FilteredComboBox<ProjectCode>
public class DataGridViewFilteredComboBoxCell : DataGridViewTextBoxCell
{
    private List<ProjectCode> _projectCodes;

    public DataGridViewFilteredComboBoxCell()
    {
        // Initialize with sample data - replace with your data source
        _projectCodes = new List<ProjectCode>
        {
            new ProjectCode { Code = 1001, Description = "Project Alpha" },
            new ProjectCode { Code = 1002, Description = "Project Beta" },
            new ProjectCode { Code = 1003, Description = "Project Gamma" }
        };
    }

    public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
        DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

        if (DataGridView.EditingControl is FilteredComboBox<ProjectCode> combo)
        {
            combo.DataSource = _projectCodes;
            combo.DisplayMember = "Description";
            combo.ValueMember = "Code";

            if (initialFormattedValue != null)
            {
                if (initialFormattedValue is int code)
                {
                    combo.SelectedValue = code;
                }
                else if (int.TryParse(initialFormattedValue.ToString(), out int parsedCode))
                {
                    combo.SelectedValue = parsedCode;
                }
            }
        }
    }

    public override Type EditType => typeof(FilteredComboBox<ProjectCode>);
    public override Type ValueType => typeof(int);
    public override object DefaultNewRowValue => null;

    protected override object GetFormattedValue(object value,
        int rowIndex,
        ref DataGridViewCellStyle cellStyle,
        TypeConverter valueTypeConverter,
        TypeConverter formattedValueTypeConverter,
        DataGridViewDataErrorContexts context)
    {
        if (value is int code)
        {
            var project = _projectCodes.FirstOrDefault(p => p.Code == code);
            return project?.ToString() ?? code.ToString();
        }
        return value?.ToString() ?? string.Empty;
    }
}

// Placeholder for CheckedComboBox cell (implement similarly)
public class DataGridViewCheckedComboBoxCell : DataGridViewTextBoxCell
{
    // Implementation would be similar to FilteredComboBoxCell
    // but for CheckedComboBox functionality
    public override Type EditType => typeof(CheckedComboBox);
    // ... other required overrides ...
}