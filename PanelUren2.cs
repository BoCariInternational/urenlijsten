using System;
using System.Windows.Forms;
using System.Data;
using CustomControls;
using System.ComponentModel;

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
        // ... [keep all existing fields and methods] ...

        private void AddColumns()
        {
            // ... [keep existing column additions] ...

            // Updated Projectnummer column using FilteredComboBox<ProjectCode>
            var projectNummerColumn = new DataGridViewColumn
            {
                Name = "Projectnummer",
                HeaderText = "Projectnummer",
                CellTemplate = new DataGridViewFilteredComboBoxCell()
            };
            dataGridView1.Columns.Add(projectNummerColumn);

            // ... [keep rest of existing columns] ...
        }

        // ... [keep rest of existing methods] ...
    }

    public class ProjectCode
    {
        public int Code { get; set; }
        public string Description { get; set; }

        public override string ToString() => $"{Code} - {Description}";
    }

    public class DataGridViewFilteredComboBoxCell : DataGridViewTextBoxCell
    {
        private List<ProjectCode> _projectCodes;

        public DataGridViewFilteredComboBoxCell()
        {
            // Initialize with sample data - replace with your actual data source
            _projectCodes = new List<ProjectCode>
        {
            new ProjectCode { Code = 1001, Description = "Websitedevelopment" },
            new ProjectCode { Code = 1002, Description = "Mobile App" },
            new ProjectCode { Code = 1003, Description = "Database Migratie" }
        };
        }

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

            if (DataGridView.EditingControl is FilteredComboBox<ProjectCode> comboBox)
            {
                comboBox.DataSource = _projectCodes;
                comboBox.DisplayMember = "Description";
                comboBox.ValueMember = "Code";

                if (initialFormattedValue != null)
                {
                    if (initialFormattedValue is int code)
                    {
                        comboBox.SelectedValue = code;
                    }
                    else if (initialFormattedValue is string strValue && int.TryParse(strValue, out int parsedCode))
                    {
                        comboBox.SelectedValue = parsedCode;
                    }
                }
            }
        }

        public override Type EditType => typeof(FilteredComboBox<ProjectCode>);
        public override Type ValueType => typeof(int); // We store just the Code
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

        public override object ParseFormattedValue(object formattedValue,
            DataGridViewCellStyle cellStyle,
            TypeConverter formattedValueTypeConverter,
            TypeConverter valueTypeConverter)
        {
            if (formattedValue is ProjectCode pc)
            {
                return pc.Code;
            }
            if (formattedValue is string str)
            {
                if (int.TryParse(str, out int code))
                    return code;

                // Try to extract code from "XXX - Description" format
                var dashIndex = str.IndexOf('-');
                if (dashIndex > 0 && int.TryParse(str.Substring(0, dashIndex).Trim(), out code))
                    return code;
            }
            return base.ParseFormattedValue(formattedValue, cellStyle, formattedValueTypeConverter, valueTypeConverter);
        }
    }