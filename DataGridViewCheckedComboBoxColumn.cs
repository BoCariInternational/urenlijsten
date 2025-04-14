
using System;
using System.Windows.Forms;
using System.ComponentModel; // Voor TypeConverter

namespace CustomControls
{
    // CheckedComboBox cel implementatie
    public class DataGridViewCheckedComboBoxCell : DataGridViewTextBoxCell
    {
        // Single source of truth for the combined value. Used by ParseFormattedValue aand CellEndEdit
        public string GetCombinedValue(CheckedComboBox combo)
        {
            return combo.GetCombinedValue();
        }

        // Wordt aangeroepen door de DataGridView tijdens databinding en validatie
        public override object ParseFormattedValue(
            object formattedValue,
            DataGridViewCellStyle cellStyle,
            TypeConverter formattedValueTypeConverter,
            TypeConverter valueTypeConverter)
        {
            if (DataGridView.EditingControl is CheckedComboBox combo)
            {
                return GetCombinedValue(combo);
            }
            return string.Empty;
        }

        public override void InitializeEditingControl(int rowIndex, object /* formatted (= short) string*/ initialFormattedValue,
            DataGridViewCellStyle cellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, cellStyle);

            /*
            Onderstaande code is "te vroeg".
            Vlak na deze method wordt dv_EditingControlShowin aangeroepen.
            In geval van kolom "Projecttype" wordt comboProjectType.SetDataSource aangeroepen.
            Deze cleart de selectie...

            if (DataGridView.EditingControl is CheckedComboBox combo)
            {
                int projectTypeColumnIndex = this.ColumnIndex; // DataGridView.Columns["Projecttype"].Index;
                object cellValue = DataGridView.Rows[rowIndex].Cells[projectTypeColumnIndex].Value;

                if (cellValue is string combinedValue && !string.IsNullOrEmpty(combinedValue))
                {
                    string[] parts = combinedValue.Split(';');
                    if (parts.Length == 2)
                    {
                        //string shortNames = parts[0];
                        string longNames = parts[1];
                        combo.SetCheckedItems(longNames.Split(',').ToList());
                        // Gebruik shortName en longName om je combo te initialiseren
                        // Bijvoorbeeld, de juiste items selecteren in de dropdown.
                    }
                }
            }
            */
        }

        protected override object GetFormattedValue(
            object value,
            int rowIndex,
            ref DataGridViewCellStyle cellStyle,
            TypeConverter valueTypeConverter,
            TypeConverter formattedValueTypeConverter,
            DataGridViewDataErrorContexts context)
        {
            string combinedValue = value?.ToString();
            if (!string.IsNullOrEmpty(combinedValue))
            {
                // Toon alleen het deel vóór de ';' (afgekort)
                return combinedValue.Split(';')[0];
            }
            return string.Empty;
        }

        public override Type EditType => typeof(CheckedComboBox);
        public override Type ValueType => typeof(string);
        public override object DefaultNewRowValue => string.Empty;
    }

    public class DataGridViewCheckedComboBoxColumn : DataGridViewColumn
    {
        public DataGridViewCheckedComboBoxColumn()
            : base(new DataGridViewCheckedComboBoxCell())
        {
        }
    }
}// namespace CustomControls
