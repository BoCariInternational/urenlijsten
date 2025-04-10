using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Timers;

namespace CustomControls
{
    public class FilteredComboBox<T> : ComboBox
    {
        private const int FILTER_DELAY_MS = 500; // Configurable delay
        private List<T> _sourceList;
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

        // Property for the source list
        public List<T> SourceList
        {
            get => _sourceList;
            set
            {
                _sourceList = value;
                ApplyFilter(string.Empty); // Initialize with full list
            }
        }

        // Gets the currently selected item of type T
        public new T SelectedItem => (T)base.SelectedItem;

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

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                    if (!this.DroppedDown) this.DroppedDown = true;
                    break;

                case Keys.Tab:
                case Keys.Enter:
                    if (this.Items.Count > 0 && !string.IsNullOrEmpty(this.Text))
                    {
                        this.SelectedIndex = 0; // Auto-select top item
                        e.Handled = true; // Prevent further processing
                    }
                    break;
            }
        }

        private void OnLostFocus(object sender, EventArgs e)
        {
            _filterTimer.Stop();
        }

        private void ApplyFilter(string filterText)
        {
            if (_sourceList == null || _isFilteringInProgress) return;

            // Don't re-filter if the text hasn't changed
            if (filterText == _lastFilterText) return;

            _isFilteringInProgress = true;
            try
            {
                _lastFilterText = filterText;

                // Build regex pattern with .+ between terms (requires consecutive chars)
                string regexPattern = string.IsNullOrEmpty(filterText)
                    ? ".*"
                    : ".*" + string.Join(".+", filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) + ".*";

                var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

                var filteredItems = _sourceList
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
                        this.DisplayMember = "";

                        if (filteredItems.Count == 1)
                        {
                            this.SelectedIndex = 0;
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
}// namespace CustomControls
