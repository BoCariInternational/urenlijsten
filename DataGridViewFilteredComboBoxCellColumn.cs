using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel; // Voor TypeConverter

namespace CustomControls
{
    public interface IDataGridViewUserControl
    {
        public void InitControl(object value);
        public string GetFormattedValue(object value);
    };

    // --- DE CUSTOM CELL ---


    public class FilteredComboBoxCell<TItem> : DataGridViewComboBoxCell
    {
        private object _cellValue;

        protected override object GetFormattedValue(
            object value,
            int rowIndex,
            ref DataGridViewCellStyle cellStyle,
            TypeConverter valueTypeConverter,
            TypeConverter formattedValueTypeConverter,
            DataGridViewDataErrorContexts context)
        {
            // _cellValue = value;
            // return value?.ToString() ?? string.Empty; //control.GetFormattedValue(value);


            if (this.DataGridView.EditingControl is FilteredComboBox<TItem> control)
            {
                _cellValue = value; // Bewaar de waarde voor later gebruik
                return control.GetFormattedValue(value);
            }
            else
            {
                _cellValue = null;
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
                control.InitControl(_cellValue);
            }
        }

        // Override EditType om aan te geven welke control we gebruiken
        public override Type EditType => typeof(FilteredComboBox<TItem>);

        // Override ValueType om het type van de onderliggende celwaarde aan te geven
        public override Type ValueType => typeof(object);

        public override Type FormattedValueType => typeof(string);

        // Vergeet niet Clone te implementeren als je custom properties hebt
        public override object Clone()
        {
            var clone = (FilteredComboBoxCell<TItem>)base.Clone();
            // Kopieer hier eventuele custom cell properties
            clone._cellValue = _cellValue;
            return clone;
        }
    }
}
