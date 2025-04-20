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

        public int EditingControlRowIndex
        {
            get => _rowIndex;
            set => _rowIndex = value;
        }

        public bool EditingControlValueChanged
        {
            get => _valueChanged;
            set => _valueChanged = value;
        }

        // 2. Value Management
        public object EditingControlFormattedValue
        {
            get => this.Text;
            set => this.Text = value?.ToString() ?? string.Empty;
        }

        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
            => EditingControlFormattedValue;

        // 3. Visual Styling
        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            Font = dataGridViewCellStyle.Font;
            ForeColor = dataGridViewCellStyle.ForeColor;
            BackColor = SystemColors.Window;
        }

        public Cursor EditingPanelCursor => Cursors.Default;

        // 4. Behavior Control (alphabetically sorted)
        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Back:
                case Keys.Delete:
                case Keys.Down:
                case Keys.Enter:
                case Keys.Escape:
                case Keys.Left:
                case Keys.Right:
                case Keys.Space:
                case Keys.Tab:
                case Keys.Up:
                    return true;
                default:
                    return !dataGridViewWantsInputKey;
            }
        }

        public void PrepareEditingControlForEdit(bool selectAll)
        {
        }

        public bool RepositionEditingControlOnValueChange => false;

        // =============================================
        // Internal overrides
        // =============================================
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                base.OnKeyDown(e);  // Let ComboBox handle it
            }
        }
    }
}
