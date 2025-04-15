using System;
using System.Collections.Generic;
using System.Linq; // Nodig voor FirstOrDefault
using System.Windows.Forms;
using System.ComponentModel; // Voor TypeConverter

namespace CustomControls // Zorg dat namespace overeenkomt
{
    // --- DE CUSTOM CELL ---
    public class FilteredComboBoxCell : DataGridViewComboBoxCell // Afleiden van ComboBoxCell is vaak handig
    {
        object _theValue = null; // De waarde die we in de cel willen opslaan
        FilteredComboBoxColumn _dataGridViewColumn; // De kolom waartoe deze cel behoort. Deze heeft een SourceList
        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Basis implementatie aanroepen
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

            // Verkrijg een referentie naar de aangemaakte editing control
            if (this.DataGridView.EditingControl is FilteredComboBox<T> editingControl)
            {
                // --- HIER WORDT DE TEXT GEÏNITIALISEERD ---
                // Stel de Text property van de ComboBox in op basis van de waarde
                // die al in de DataGridView-cel staat.
                // 'initialFormattedValue' bevat de waarde zoals die in de cel wordt weergegeven.
                editingControl.Text = initialFormattedValue?.ToString() ?? string.Empty;
                // ------------------------------------------

                // --- Overige Initialisatie (zoals SourceList) ---
                // Haal de kolom op om de SourceList in te stellen
                if (this.OwningColumn is FilteredComboBoxColumn<T> column)
                {
                    editingControl.SourceList // FilteredComboBox property
                        = column.SourceList;
                }

                editingControl.ApplyFilter(initialFormattedValue.ToString());
                editingControl.DropDownStyle = ComboBoxStyle.DropDown;
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
            _theValue = null;
            if (this.DataGridView.EditingControl is FilteredComboBox<T> editingControl)
            {
                _theValue = value; // Bewaar de waarde voor later gebruik
            }
            else
            {
                _theValue = null;
            }

            string s = value?.ToString() ?? string.Empty;  //RR! debug
            return value?.ToString() ?? string.Empty;
        }

        // Override EditType om aan te geven welke control we gebruiken
        public override Type EditType => typeof(FilteredComboBox<T>);

        // Override ValueType om het type van de onderliggende celwaarde aan te geven
        // Als je T opslaat, gebruik typeof(T). Als je strings opslaat, typeof(string).
        public override Type ValueType => typeof(T); // Aangepast aan "gedraagt zich als textbox"

        // Override FormattedValueType om aan te geven wat er getoond wordt
        // Override GetFormattedValue om de waarde te formatteren
        public override Type FormattedValueType => typeof(string);

        // Vergeet niet Clone te implementeren als je custom properties hebt
        public override object Clone()
        {
            FilteredComboBoxCell<T> cell = (FilteredComboBoxCell<T>)base.Clone();
            // Kopieer hier eventuele custom cell properties
            return cell;
        }
    }

    // --- DE CUSTOM COLUMN (in hetzelfde bestand) ---
    public class FilteredComboBoxColumn<T> : DataGridViewColumn
    {
        private List<T> _sourceList; // De lijst met items die de edit-combobox moet gebruiken
        public List<T> SourceList
        {
            get => _sourceList;
            set
            {
                _sourceList = value;
            }
        }

        public FilteredComboBoxColumn<T>()
        {
            this.CellTemplate = new FilteredComboBoxCell(this);
    }
}
}