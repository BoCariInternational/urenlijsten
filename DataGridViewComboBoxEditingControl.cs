using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Drawing;
using System.Windows.Forms;


public class TrapComboBox : ComboBox
{
    public new object? SelectedItem
    {
        get => base.SelectedItem;
        set
        {
            System.Diagnostics.Debugger.Break(); // <--- hier knalt je debugger erin
            base.SelectedItem = value;
        }
    }

    public new int SelectedIndex
    {
        get => base.SelectedIndex;
        set
        {
            System.Diagnostics.Debugger.Break();
            base.SelectedIndex = value;
        }
    }
}

namespace CustomControls
{
    // Basisklasse voor ComboBox-afgeleide controls
    // Uses CRTP, TDerived = FilteredComboBox
    public abstract class DataGridViewComboBoxEditingControl<TDerived> : TrapComboBox,
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
            // bool dataGridViewWantsInputKey: geeft aan of de DataGridView vindt dat zijzelf die toets moet 
            // verwerken (true = dat wil ze doen, false = ze laat het over aan jou, tenzij je zegt van niet).

            // Sta alle toetsen toe die nodig zijn voor een volledige combobox ervaring
            Keys keyCode = keyData & Keys.KeyCode;

            switch (keyCode)
            {
                // Cursorbeweging en tekstbewerking
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Delete:
                case Keys.Back:
                case Keys.Space:

                // Letters
                case >= Keys.A and <= Keys.Z:

                // Cijfers bovenin toetsenbord
                case >= Keys.D0 and <= Keys.D9:

                // Numpad cijfers
                case >= Keys.NumPad0 and <= Keys.NumPad9:
                    return true;

                // Laat deze aan de DataGridView
                case Keys.Enter:
                case Keys.Escape:
                case Keys.Tab:
                    return false;

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
