
using System;
using System.Windows.Forms;
using System.ComponentModel; // Voor TypeConverter

namespace CustomControls
{
    // CheckedComboBox cel implementatie
    public class DataGridViewCheckedComboBoxCell : DataGridViewTextBoxCell
    {
        public override object ParseFormattedValue(
            object formattedValue,
            DataGridViewCellStyle cellStyle,
            TypeConverter formattedValueTypeConverter,
            TypeConverter valueTypeConverter)
        {
            if (DataGridView.EditingControl is CheckedComboBox combo)
            {
                string displayValues = string.Join(",", combo.GetCheckedValuesShort()); // Afgekort
                string realValues = string.Join(",", combo.GetCheckedValues());        // Volledig
                return $"{displayValues};{realValues}";
            }
            return string.Empty;
        }

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
            DataGridViewCellStyle cellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, cellStyle);

            if (DataGridView.EditingControl is CheckedComboBox combo)
            {
                string combinedValue = initialFormattedValue?.ToString();
                if (!string.IsNullOrEmpty(combinedValue))
                {
                    string realValuesPart = combinedValue.Split(';').Length > 1
                        ? combinedValue.Split(';')[1]
                        : string.Empty;

                    if (!string.IsNullOrEmpty(realValuesPart))
                    {
                        combo.SetCheckedItems(realValuesPart.Split(',').ToList());
                    }
                }
            }
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
