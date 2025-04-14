
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CustomControls
{
    public partial class CheckedComboBox : IDataGridViewEditingControl
    {
        private DataGridView dataGridView;
        private bool valueChanged;
        private int rowIndex;

        public object EditingControlFormattedValue
        {
            get => GetCombinedValue();
            set => _textBox.Text = "Blah"; //value?.ToString() ?? ""; //RR!!
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
            get => dataGridView;
            set => dataGridView = value; // Het DataGridView vult dit zelf in!
        }

        public Cursor EditingPanelCursor => Cursors.Default;

        public void PrepareEditingControlForEdit(bool selectAll)
        {
            _textBox.Focus();
            if (selectAll)
                _textBox.SelectAll();
        }

        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Enter:
                case Keys.Escape:
                case Keys.Space:
                    return true;
                default:
                    return !dataGridViewWantsInputKey;
            }
        }
    }
}
