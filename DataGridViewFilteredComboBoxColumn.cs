
using System;
using System.Windows.Forms;
using System.ComponentModel;

namespace CustomControls
{
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
}
