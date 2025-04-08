using System;
using System.Windows.Forms;
using System.Data;
using CustomControls;

namespace Urenlijsten_App
{
    public class ProjectCode
    {
        public int Code { get; set; }
        public string Description { get; set; }

        // Override ToString for display in combobox
        public override string ToString()
        {
            return $"{Code} - {Description}";
        }
    }
}

namespace CustomControls
{
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
        }

        private void AddColumns()
        {
            // Klantnaam column (string)
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Klantnaam",
                HeaderText = "Klantnaam",
                DataPropertyName = "Klantnaam"
            });

            // Projecttype column (CheckedComboBox)
            var projectTypeColumn = new DataGridViewColumn
            {
                Name = "Projecttype",
                HeaderText = "Projecttype",
                CellTemplate = new DataGridViewCheckedComboBoxCell()
            };
            dataGridView1.Columns.Add(projectTypeColumn);

            // Projectnummer column (FilteredComboBox)
            var projectNummerColumn = new DataGridViewColumn
            {
                Name = "Projectnummer",
                HeaderText = "Projectnummer",
                CellTemplate = new DataGridViewFilteredComboBoxCell()
            };
            dataGridView1.Columns.Add(projectNummerColumn);

            // km dienstreis column (numeric, integers only)
            var kmColumn = new DataGridViewTextBoxColumn
            {
                Name = "kmDienstreis",
                HeaderText = "km dienstreis",
                DataPropertyName = "kmDienstreis"
            };
            dataGridView1.Columns.Add(kmColumn);

            // Add day columns (Ma, Di, Wo, Do, Vr, Za, Zo)
            AddDayColumn("Ma");
            AddDayColumn("Di");
            AddDayColumn("Wo");
            AddDayColumn("Do");
            AddDayColumn("Vr");
            AddDayColumn("Za");
            AddDayColumn("Zo");

            // Totaal column (computed, read-only)
            var totaalColumn = new DataGridViewTextBoxColumn
            {
                Name = "Totaal",
                HeaderText = "Totaal",
                ReadOnly = true,
                DataPropertyName = "Totaal"
            };
            dataGridView1.Columns.Add(totaalColumn);

            // Set up cell validation
            dataGridView1.CellValidating += DataGridView1_CellValidating;
            dataGridView1.EditingControlShowing += DataGridView1_EditingControlShowing;
        }

        private void AddDayColumn(string dayName)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = dayName,
                HeaderText = dayName,
                DataPropertyName = dayName
            };
            dataGridView1.Columns.Add(column);
        }

        private void DataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            // Validate km dienstreis column (must be integer)
            if (dataGridView1.Columns[e.ColumnIndex].Name == "kmDienstreis")
            {
                if (!string.IsNullOrEmpty(e.FormattedValue.ToString()))
                {
                    if (!int.TryParse(e.FormattedValue.ToString(), out _))
                    {
                        e.Cancel = true;
                        MessageBox.Show("Voer alleen hele getallen in voor km dienstreis.");
                    }
                }
            }

            // Validate day columns (must be whole or half)
            string[] days = { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
            if (Array.Exists(days, day => day == dataGridView1.Columns[e.ColumnIndex].Name))
            {
                if (!string.IsNullOrEmpty(e.FormattedValue.ToString()))
                {
                    string value = e.FormattedValue.ToString();

                    // Check for decimal point (only .5 allowed)
                    if (value.Contains("."))
                    {
                        if (value != "0.5" && value != "1.5" && value != "2.5" &&
                            value != "3.5" && value != "4.5" && value != "5.5" && value != "6.5" &&
                            value != "7.5" && value != "8.5")
                        {
                            e.Cancel = true;
                            MessageBox.Show("Voer alleen hele uren of .5 in voor halve uren.");
                        }
                    }
                    else
                    {
                        if (!int.TryParse(value, out _))
                        {
                            e.Cancel = true;
                            MessageBox.Show("Voer alleen cijfers in voor uren.");
                        }
                    }
                }
            }
        }

        private void DataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            // Handle day columns to prevent typing after decimal point
            string[] days = { "Ma", "Di", "Wo", "Do", "Vr", "Za", "Zo" };
            var columnName = dataGridView1.Columns[dataGridView1.CurrentCell.ColumnIndex].Name;

            if (Array.Exists(days, day => day == columnName) && e.Control is TextBox textBox)
            {
                textBox.KeyPress += (s, ev) =>
                {
                    // Allow numbers, backspace, and single decimal point
                    if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar) && ev.KeyChar != '.')
                    {
                        ev.Handled = true;
                    }

                    // Only allow one decimal point
                    if (ev.KeyChar == '.' && ((TextBox)s).Text.IndexOf('.') > -1)
                    {
                        ev.Handled = true;
                    }

                    // If there's already a decimal point, don't allow more input
                    if (((TextBox)s).Text.Contains(".") &&
                        ((TextBox)s).SelectionStart > ((TextBox)s).Text.IndexOf('.'))
                    {
                        ev.Handled = true;
                    }
                };
            }

            // Handle km column to allow only numbers
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

    // Custom cell classes for the specialized controls
    public class DataGridViewCheckedComboBoxCell : DataGridViewTextBoxCell
    {
        public DataGridViewCheckedComboBoxCell() : base()
        {
            // You would implement this to use the CheckedComboBox control
            // This is a simplified placeholder
        }

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
            var ctrl = DataGridView.EditingControl as CheckboxComboBox;
            if (ctrl != null)
            {
                // Initialize the CheckedComboBox here
            }
        }

        public override Type EditType => typeof(CheckboxComboBox);
        public override Type ValueType => typeof(string);
        public override object DefaultNewRowValue => null;
    }

    public class DataGridViewFilteredComboBoxCell : DataGridViewTextBoxCell
    {
        public DataGridViewFilteredComboBoxCell() : base()
        {
            // You would implement this to use the FilteredComboBox control
            // This is a simplified placeholder
        }

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
            var ctrl = DataGridView.EditingControl as FilteredComboBox;
            if (ctrl != null)
            {
                // Initialize the FilteredComboBox here
            }
        }

        public override Type EditType => typeof(FilteredComboBox);
        public override Type ValueType => typeof(string);
        public override object DefaultNewRowValue => null;
    }
}

