
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CustomControls
{
    // Basisklasse voor ComboBox-afgeleide controls
    // Uses CRTP, TDerived = FilteredComboBox
    public abstract class DataGridViewComboBoxEditingControl<TDerived> : ComboBox,
        IDataGridViewEditingControl
        where TDerived : DataGridViewComboBoxEditingControl<TDerived>
    {
        // Private fields
        private DataGridView _internalDataGridView;
        private bool _valueChanged;
        private int _rowIndex;

        // =============================================
        // IDataGridViewEditingControl Interface Members
        // =============================================

        // 1. Core Properties
        public DataGridView EditingControlDataGridView
        {
            get => _internalDataGridView;
            set => _internalDataGridView = value;
        }

        public bool EditingControlValueChanged
        {
            get => _valueChanged;
            set => _valueChanged = value;
        }

        public int EditingControlRowIndex
        {
            get => _rowIndex;
            set => _rowIndex = value;
        }

        // 2. Value Management
        public object EditingControlFormattedValue
        {
            get => this.Text;
            set => this.Text = value?.ToString() ?? string.Empty; //RR!!
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
            => EditingControlFormattedValue;

        // 3. Visual Styling
        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            Font = dataGridViewCellStyle.Font;
            ForeColor = dataGridViewCellStyle.ForeColor;
            BackColor = dataGridViewCellStyle.BackColor;
        }

        public Cursor EditingPanelCursor => Cursors.Default;

        // 4. Behavior Control
        public bool RepositionEditingControlOnValueChange => false;

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
                    return !dataGridViewWantsInputKey;
            }
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
            // Standard implementation (can be overridden)
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                base.OnKeyDown(e);  // Let ComboBox handle it
            }
        }
    }
}