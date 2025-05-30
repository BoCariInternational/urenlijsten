using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel; // Voor TypeConverter

namespace CustomControls
{
    public interface IDataGridViewUserControl
    {
        public void InitControl();
        public string GetFormattedValue(object value);
    };

    // --- DE CUSTOM CELL ---


    public class FilteredComboBoxCell<TItem> : DataGridViewComboBoxCell
        where TItem : class
    {
        protected override object GetFormattedValue(
            object value,
            int rowIndex,
            ref DataGridViewCellStyle cellStyle,
            TypeConverter valueTypeConverter,
            TypeConverter formattedValueTypeConverter,
            DataGridViewDataErrorContexts context)
        {
            if (value != null && this.DataGridView.EditingControl is FilteredComboBox<TItem> control)
            {
                return control.GetFormattedValue(value);
            }
            else
            {
                return string.Empty;
            }
        }

        public override void InitializeEditingControl(
            int rowIndex,
            object initialFormattedValue,
            DataGridViewCellStyle cellStyle)
        {
            base.InitializeEditingControl(rowIndex, initialFormattedValue, cellStyle);

            if (DataGridView.EditingControl is FilteredComboBox<TItem> control)
            {
                control.InitControl();
            }
        }

        // Override EditType om aan te geven welke control we gebruiken
        public override Type EditType => typeof(FilteredComboBox<TItem>); //RR!

        // Override ValueType om het type van de onderliggende celwaarde aan te geven
        public override Type ValueType => typeof(object);

        public override Type FormattedValueType => typeof(string);

        // Vergeet niet Clone te implementeren als je custom properties hebt
        public override object Clone()
        {
            var clone = (FilteredComboBoxCell<TItem>)base.Clone();
            // Kopieer hier eventuele custom cell properties
            return clone;
        }
    }
}
