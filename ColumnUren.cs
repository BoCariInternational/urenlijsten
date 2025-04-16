using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomControls;

namespace Urenlijsten_App
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using static Urenlijsten_App.PanelUren;

    public class ColumnUrenCode : DataGridViewColumn
    {
        public ColumnUrenCode() : base(new FilteredComboBoxCell<ProjectItem>()) // Default to TextBoxCell
        {
        }

        public override DataGridViewCell CellTemplate
        {
            get
            {
                return base.CellTemplate;
            }
            set
            {
                // Only allow our custom cell type
                //if (value != null &&
                //!value.GetType().IsAssignableFrom(typeof(ComboBox))) //RR!
                //{
                //    throw new InvalidCastException("Can only assign a CustomDataGridViewTextBoxCell to the CellTemplate.");
                //}

                base.CellTemplate = value;
            }
        }
        /*
        // RR!! RowIndex niet bekend
        public override Type EditType
        {
            get
            {
                // Bepaal het edit control type op basis van de rijindex
                if (this.DataGridView != null && this.RowIndex >= 0)
                {
                    if (this.RowIndex < 10) // Voorbeeldvoorwaarde
                    {
                        return typeof(DataGridViewComboBoxEditingControl);
                    }
                    else
                    {
                        return typeof(DataGridViewTextBoxEditingControl);
                    }
                }
                return base.EditType;
            }
        }
        */
    }

    // Assuming you have a FilteredComboBoxCell defined elsewhere, for example:
    public class FilteredComboBoxCell<TItem> : DataGridViewComboBoxCell
{
    // Add your specific implementation for the FilteredComboBoxCell here
    public FilteredComboBoxCell()
    {
        // Example: Initialize some items

    }

    // You might need to override methods like InitializeEditingControl if your filtering logic requires it.
}
}
