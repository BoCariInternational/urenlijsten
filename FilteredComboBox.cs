using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Timers;
using DocumentFormat.OpenXml.Office2013.PowerPoint.Roaming;

namespace CustomControls
{
    public partial class FilteredComboBox<TItem> : DataGridViewComboBoxEditingControl<FilteredComboBox<TItem>>, IDataGridViewUserControl
    {
        public static List<TItem> _projectItems;
        private const int FILTER_DELAY_MS = 500; // Configurable delay
        private List<TItem> _sourceList;
        private System.Timers.Timer _filterTimer;
        private string _lastFilterText = string.Empty;
        private bool _isFilteringInProgress = false;

        public FilteredComboBox()
        {
            this.DropDownStyle = ComboBoxStyle.DropDown;
            this.AutoCompleteMode = AutoCompleteMode.None; // We handle this ourselves

            // Set up the filter delay timer
            _filterTimer = new System.Timers.Timer(FILTER_DELAY_MS);
            _filterTimer.AutoReset = false;
            _filterTimer.Elapsed += OnFilterTimerElapsed;

            this.TextChanged += OnTextChanged;
            this.KeyDown += OnKeyDown;
            this.LostFocus += OnLostFocus;
        }

        public void InitControl(object value)
        {
            DataSource = _projectItems;
            ApplyFilter(string.Empty);
            this.DisplayMember = "ToString";
            this.ValueMember = "GetItem";
        }
        public string GetFormattedValue(object value)
        {
            return value == null ? string.Empty : value.ToString(); // Return the formatted value as a string
        }

        // Gets the currently selected item of type T
        // public new T SelectedItem => (T)base.SelectedItem;

        private void OnTextChanged(object sender, EventArgs e)
        {
            // Reset the timer on each keystroke
            _filterTimer.Stop();

            if (!string.IsNullOrEmpty(this.Text))
            {
                _filterTimer.Start();

                // Auto-open dropdown if not already open
                if (!this.DroppedDown)
                {
                    this.DroppedDown = true;
                }
            }
            else
            {
                ApplyFilter(string.Empty);
            }
        }

        private void OnFilterTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // This runs on a timer thread, so we need to invoke on the UI thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ApplyFilter(this.Text)));
            }
            else
            {
                ApplyFilter(this.Text);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e) // Event handler
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                    this.DroppedDown = !this.DroppedDown; // Toggle dropdown on arrow keys
                    e.Handled = true;
                    break;

                case Keys.Tab:
                case Keys.Enter:
                    if (this.Items.Count > 0 && !string.IsNullOrEmpty(this.Text))
                    {
                        this.SelectedIndex = 0; // Auto-select top item

                        // Markeer dat er wijzigingen zijn
                        // Anders slaat de grid cell de wijziging niet op.
                        this.EditingControlValueChanged = true;               // RR!! Mind what Gemini said.

                        // Forceer validatie van de cel (edit widget)
                        this.EditingControlDataGridView.EndEdit();            // Retourneert false als validatie faalt
                        // Start deze sequentie:
                        //1. EditingControl.GetEditingControlFormattedValue() // Haal de waarde op
                        //2. DataGridView.CellValidating                      // Validatie-event
                        //→ Als e.Cancel = true: stopt het proces
                        //3. DataGridView.CellValidated                       // Na succesvolle validatie
                        //4. DataGridView.CellEndEdit                         // Finalisatie
                        e.Handled = true;
                    }
                    break;
                case Keys.Escape:
                    this.EditingControlValueChanged = false;

                    //this.EditingControlDataGridView.BeginInvoke(new Action(() => this.EditingControlDataGridView.EndEdit()));
                    //this.EditingControlDataGridView.EndEdit();  // De wijziging wordt NIET opgeslagen
                    e.Handled = false;                            // Laat de escape door om de dropdown te sluiten
                    break;
            }

            // Zonder de base.OnKeyDown(e) roep je de standaardverwerking van ComboBox niet aan
            // Echter: we gebruiken nu CRTP en onze eigen base class zit tussen deze en Combobox in.
            // Het equivalent van ComboBox::OnKeyDown(e); is niet mogelijk in C#
            // Dus DataGridViewComboBoxEditingControl<FilteredComboBox<TItem> sluist het door naar ComboBox.
            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        private void OnLostFocus(object sender, EventArgs e)
        {
            _filterTimer.Stop();
        }

        public void ApplyFilter(string filterText)
        {
            if (_projectItems == null || _isFilteringInProgress) return; //_sourelist is overbodig, gebruik _projectitems

            // Don't re-filter if the text hasn't changed
            if (filterText == _lastFilterText) return;

            _isFilteringInProgress = true;
            try
            {
                _lastFilterText = filterText;

                // Build regex pattern with .+ between terms (requires consecutive chars)
                string regexPattern = string.IsNullOrWhiteSpace(filterText)
                    ? ".*"
                    : ".*" + string.Join(".+", filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) + ".*";

                var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

                var filteredItems = _projectItems
                    .Where(item => regex.IsMatch(item.ToString()))
                    .ToList();

                this.BeginUpdate();
                try
                {
                    this.DataSource = null;
                    this.Items.Clear();

                    if (filteredItems.Count > 0)
                    {
                        this.DataSource = filteredItems;
                        this.SelectedIndex = -1; // Reset selection
                        this.Text = filterText;  // Set the text to the filter text

                        if (filteredItems.Count == 1)
                        {
                            var e = new KeyEventArgs(Keys.Enter);
                            this.OnKeyDown(e);
                            // Dit triggert alleen mijn eigen override van OnKeyDown, 
                            // niet eventuele event handlers van buitenaf of andere events zoals KeyPress
                        }
                    }

                    if (!string.IsNullOrEmpty(filterText)
                        && filteredItems.Count > 1
                        && this.Focused)
                    {
                        this.DroppedDown = true;
                    }
                }
                finally
                {
                    this.EndUpdate();
                }
            }
            finally
            {
                _isFilteringInProgress = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _filterTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}// namespace Urenlijsten_App
