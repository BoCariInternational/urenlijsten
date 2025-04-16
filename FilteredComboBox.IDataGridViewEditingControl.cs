
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CustomControls
{
    public partial class FilteredComboBox<TItem> : DataGridViewComboBoxEditingControl<FilteredComboBox<TItem>>, IDataGridViewUserControl
    {
        private DataGridView internalDataGridView;
        private bool valueChanged;
        private int rowIndex;

        public object EditingControlFormattedValue
        {
            get => this.Text;
            set => this.Text = value?.ToString() ?? string.Empty;
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
            => EditingControlFormattedValue;

        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            Font = dataGridViewCellStyle.Font;
            ForeColor = dataGridViewCellStyle.ForeColor;
            BackColor = dataGridViewCellStyle.BackColor;
        }

        public int EditingControlRowIndex
        {
            get => rowIndex;
            set => rowIndex = value;
        }

        public bool EditingControlValueChanged
        {
            get => valueChanged;
            set => valueChanged = value;
        }

        public bool RepositionEditingControlOnValueChange => false;

        public DataGridView EditingControlDataGridView
        {
            get => internalDataGridView;
            set => internalDataGridView = value; // Het DataGridView vult dit zelf in!
        }

        public Cursor EditingPanelCursor => Cursors.Default;

        public void PrepareEditingControlForEdit(bool selectAll)
        {
            this.Focus();
        }

        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Tab:
                case Keys.Back:
                case Keys.Delete:
                case Keys.Enter:
                case Keys.Escape:
                case Keys.Space:
                    return true;
                default:
                    // Laat de DataGridView het verwerken
                    return !dataGridViewWantsInputKey;
            }
        }
    }
}
