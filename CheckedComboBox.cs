using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Urenlijsten_App;
using Time = System.Windows.Forms;

namespace CustomControls
{
    public interface IShortNameable
    {
        string ToString();      // Long name for checkboxes
        string ToStringShort(); // Short name for textbox
    }

    public partial class CheckedComboBox : UserControl, IDataGridViewEditingControl
    {
        // Child controls
        private TableLayoutPanel _layoutPanel;
        private TextBox _textBox;
        private Button _btnDropDown, _btnClear, _btnSelectAll;
        private CheckedListBox _checkedListBox;
        private Panel _panelDropDown;
        private ToolTip _toolTip = new(); // Added ToolTip component
        private Time.Timer _dropdownCloseTimer = new();
        private bool startMonitoring = false;
        private bool selectionHasChanged = false;

        // Constructor
        public CheckedComboBox()
        {
            InitializeComponents();
            WireEvents();
        }

        // Public API
        public void SetDataSource<T>(List<T> items) where T : IShortNameable
        {
            // Als _checkedListBox nog geen parent heeft,
            // dan worden de _checkedListBox.Items niet ge-update!!!
            if (ParentForm != null && !ParentForm.Controls.Contains(_panelDropDown))
            {
                ParentForm.Controls.Add(_panelDropDown);
                ParentForm.Resize += ParentForm_Resize;
            }

            _checkedListBox.DataSource = items;
            ClearSelections();
        }

        private void ParentForm_Resize(object? sender, EventArgs e)
        {
            PositionDropdown(true);
        }

        // Initialization
        private void InitializeComponents()
        {
            // @formatter:off

            this.Padding = new Padding(0);
            this.Margin = new Padding(0);

            _panelDropDown = new Panel
            {
                Dock = DockStyle.None,
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle,
            };

            _checkedListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                HorizontalScrollbar = true,
            };
            _panelDropDown.Controls.Add(_checkedListBox);

            _layoutPanel = new TableLayoutPanel()
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                ColumnStyles =
                {
                    new ColumnStyle(SizeType.Percent, 100F), // _textBox
                    new ColumnStyle(SizeType.Absolute, 24),  // _btnDropDown
                    new ColumnStyle(SizeType.Absolute, 24),  // _btnClear
                    new ColumnStyle(SizeType.Absolute, 24),  // _btnSelectAll
                },
                RowStyles =
                {
                    new RowStyle(SizeType.Absolute, 24) // Or SizeType.Absolute with desired height
                },
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };

            _textBox = new TextBox()
            {
                //Dock = DockStyle.Fill,          // Or set Anchor to Left, Right
                //Padding = new Padding(0),
                //Margin = new Padding(0),
                //BackColor = Color.White,        // Force white background
                //BorderStyle = BorderStyle.None, // Remove default border
                Anchor = AnchorStyles.Left | AnchorStyles.Right,  // Horizontal stretch
                Margin = new Padding(0, 3, 0, 0),  // Top padding for visual centering
                TextAlign = HorizontalAlignment.Left,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window,
            };
            _layoutPanel.Controls.Add(_textBox, 0, 0);

            const int btnSize = 26;
            const int fontSize = 10;
            _btnDropDown = new Button
            {
                Text = "▼",
                Font = new Font("Arial", fontSize, FontStyle.Bold),
                Width = btnSize,
                Height = btnSize,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _layoutPanel.Controls.Add(_btnDropDown, 1, 0);

            // "Clear All"-Button (◻)
            _btnClear = new Button
            {
                //Text = "◻", // White medium square (U+25FB)
                //Font = new Font("Arial", fontSize, FontStyle.Regular),
                Text = "☐",  // U+2610
                Font = new Font("Segoe UI Symbol", 10, FontStyle.Regular),
                Width = btnSize,
                Height = btnSize,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Enabled = false
            };
            _btnClear.FlatAppearance.BorderSize = 0;
            //_btnClear.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            _btnClear.Click += (s, e) => ClearSelections();
            _layoutPanel.Controls.Add(_btnClear, 2, 0);

            // "Select All"-Button (◼)
            _btnSelectAll = new Button
            {
                Text = "◼",  // Black medium square (U+25FC)
                Font = new Font("Arial", fontSize, FontStyle.Regular),
                Width = btnSize,
                Height = btnSize,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Enabled = true
            };
            //_btnSelectAll.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            _btnSelectAll.FlatAppearance.BorderSize = 0;
            _btnSelectAll.Click += (s, e) => SelectAll();
            _layoutPanel.Controls.Add(_btnSelectAll, 3, 0);

            this.Controls.Add(_layoutPanel);

            // Add tooltips
            _toolTip.SetToolTip(_btnClear, "Clear All");
            _toolTip.SetToolTip(_btnSelectAll, "Select All");

            // ToolTip setup
            _toolTip.AutoPopDelay = 5000;
            _toolTip.InitialDelay = 500;

            // Ensure the dropdown panel is added to the FormUren.Current's controls
            if (ParentForm != null && !ParentForm.Controls.Contains(_panelDropDown))
            {
                ParentForm.Controls.Add(_panelDropDown);
            }
            // @formatter: on
        }

        public void ClearSelections()
        {
            for (int i = 0; i < _checkedListBox.Items.Count; i++)
            {
                _checkedListBox.SetItemChecked(i, false);
            }
            UpdateTextDisplay();
        }

        public void SelectAll()
        {
            for (int i = 0; i < _checkedListBox.Items.Count; i++)
            {
                _checkedListBox.SetItemChecked(i, true);
            }
            UpdateTextDisplay();
        }

        public void SetCheckedItems(List<string> selectedItems)
        {
            ClearSelections();

            for (int i = 0; i < _checkedListBox.Items.Count; i++)
            {
                if (_checkedListBox.Items[i] is IShortNameable item)
                {
                    _checkedListBox.SetItemChecked(i, selectedItems.Contains(item.ToString()));
                }
            }
            UpdateTextDisplay();
            this.startMonitoring = true;
        }

        public List<string> GetCheckedValues()
        {
            return _checkedListBox.CheckedItems
                .Cast<IShortNameable>()
                .Select(w => w.ToString())
                .ToList();
        }

        public string GetTextBoxText()
        {
            return _textBox.Text;
        }

        public List<string> GetCheckedValuesShort()
        {
            return _checkedListBox.CheckedItems
                .Cast<IShortNameable>()
                .Select(w => w.ToStringShort())
                .ToList();
        }

        public string GetCombinedValue()
        {
            string shortNames = GetTextBoxText();
            string longNames = string.Join(",", GetCheckedValues());
            return $"{shortNames};{longNames}";
            //return "Civ,Ele,Ins;Civil,Electrical,Instrumentation"; //RR! hack 
        }

        public bool IsPanelVisible
        {
            get { return _panelDropDown != null && _panelDropDown.Visible; }
        }

        // ---- Focus & Dropdown Management ----
        private void WireEvents()
        {
            // Click handling
            _textBox.Click += (s, e) => ToggleDropdown();
            _btnDropDown.Click += (s, e) => ToggleDropdown();

            // Focus handling
            //_textBox.GotFocus += (s, e) => OnGotFocus(e);
            //_btnDropDown.GotFocus += (s, e) => OnGotFocus(e);
            //this.LostFocus += OnThisLostFocus;

            _textBox.KeyDown += OnTextBoxKeyDown;


            // Checkbox
            _checkedListBox.ItemCheck += _checkedListBox_ItemChecked;
            _checkedListBox.SelectedIndexChanged += _checkedListBox_SelectedIndexChanged;
            _checkedListBox.KeyDown += _checkedListBox_KeyDown;
            _checkedListBox.LostFocus += _checkedListBox_LostFocus;

            // Oneshot timer met pulsverlenging
            _dropdownCloseTimer.Tick += _dropdownCloseTimer_Tick;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Handle F4 and Alt+Arrow keys for dropdown toggle
            if (keyData == Keys.F4 || keyData == (Keys.Alt | Keys.Down) || keyData == (Keys.Alt | Keys.Up))
            {
                ToggleDropdown();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _panelDropDown.Visible)
            {
                CloseDropdown();
                e.Handled = true;
            }
        }

        private void _checkedListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                CloseDropdown();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                // Optionally restore previous selection here
                CloseDropdown();
                e.Handled = true;
            }
        }

        private void _checkedListBox_ItemChecked(object sender, ItemCheckEventArgs e)
        {
            RestartDropdownCloseTimer();
            UpdateTextDisplay(e);

            //RR!! Zet eigen vlag nadat SetDataSource, ClearSelections
            // Zie ook SetCheckedItems
            if (startMonitoring)
            {
                selectionHasChanged = true;
            }
        }

        private void _checkedListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            RestartDropdownCloseTimer();
        }

        private void RestartDropdownCloseTimer()
        {
            // Pulsverlenging: herstart timer.
            if (_dropdownCloseTimer.Enabled)
            {
                _dropdownCloseTimer.Stop();
                _dropdownCloseTimer.Interval = 750;
                _dropdownCloseTimer.Enabled = true;
            }
        }

        private bool ContainsMouse()
        {
            try
            {
                var mousePos = PointToClient(Control.MousePosition); // Kan onder omstandigheden exception geven...
                //return !(mousePos.X < 0 || mousePos.X > this.Width);
                int eps = 20;
                return !(mousePos.X < 0 - eps || mousePos.X > this.Width + eps);
                //return (this.ClientRectangle.Contains(mousePos));
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void _dropdownCloseTimer_Tick(object sender, EventArgs e)
        {
            // Timer expired.
            // Dit betekent dat er geen gebruikersinteractie was met
            // de _checkedListBox: geen (de)selectie van checkboxes,
            // geen verandering van s_selectedItem (pijltjestoetsen, muis).

            // Dit maakt de timer one shot.
            if (!ContainsMouse())
            {
                _dropdownCloseTimer.Enabled = false;
                CloseDropdown();
            }
            else
            {
                // Geen userinteractie, maar nog steeds "focus".
                RestartDropdownCloseTimer();
            }
        }


        private void _checkedListBox_LostFocus(object sender, EventArgs e)
        {
            if (!ContainsMouse())
            {
                CloseDropdown();
            }
        }

        private void ToggleDropdown()
        {
            if (_panelDropDown.Visible)
                CloseDropdown();
            else
                OpenDropdown();
        }

        private void OpenDropdown()
        {
            try
            {
                PositionDropdown();
                _panelDropDown.Show();
                _panelDropDown.BringToFront();
                _dropdownCloseTimer.Enabled = true;
                RestartDropdownCloseTimer();    // Start timer voor eerste keer.
            }
            catch (Exception ex)
            {
                ;
            }
        }

        private void CloseDropdown()
        {
            _panelDropDown.Hide();
            _textBox.BackColor = SystemColors.Window;

            if (selectionHasChanged)
            {
                // Notify DataGridView
                EditingControlValueChanged = true; // Set the property
                EditingControlDataGridView?.NotifyCurrentCellDirty(true);
            }
        }

        private void PositionDropdown(bool resizing = false)
        {
            if (!_panelDropDown.Visible && resizing)
                return;

            // Determine the position of the top-left corner of your custom control on the screen
            var controlScreenPos = PointToScreen(Point.Empty);

            // Calculate the position of the bottom-left corner of your custom control on the screen
            var controlScreenPosBottom = PointToScreen(new Point(0, _textBox.Height + 8));

            // Convert screen coordinates back to form coordinates
            var controlFormPos = ParentForm.PointToClient(controlScreenPos);
            var controlFormPosBottom = ParentForm.PointToClient(controlScreenPosBottom);

            // Determine the available space below the control
            int spaceBelow = Screen.GetWorkingArea(controlScreenPosBottom).Bottom - controlScreenPosBottom.Y;

            _panelDropDown.Width = Width;

            // Calculate the desired height based on the number of items
            int itemCount = _checkedListBox.Items.Count;
            int itemHeight = _checkedListBox.ItemHeight > 0 ? _checkedListBox.ItemHeight : 18; // Use the ItemHeight of the CheckedListBox or a default value
            int preferredHeight = itemCount * itemHeight + 2;

            // Set a maximum height (e.g., 200 pixels) to prevent the dropdown from becoming too large
            const int maxHeight = 250;
            _panelDropDown.Height = Math.Max(preferredHeight, 20);
            _panelDropDown.Height = Math.Min(preferredHeight, maxHeight);

            if (spaceBelow > _panelDropDown.Height)
            {
                // Open downward: place the top-left corner of the dropdown panel
                // directly below the bottom-left corner of the custom control
                _panelDropDown.Location = controlFormPosBottom;
            }
            else
            {
                // Open upward: place the bottom-left corner of the dropdown panel
                // directly above the top-left corner of the custom control
                _panelDropDown.Location = new Point(controlFormPos.X, controlFormPos.Y - _panelDropDown.Height);
            }

            // Zet location op 0,0 voor debugging
            //_panelDropDown.Location = Point.Empty;
        }

        private void UpdateTextDisplay(ItemCheckEventArgs? e = null)
        {
            int checkedCount = e == null
                ? _checkedListBox.CheckedItems.Count
                : _checkedListBox.CheckedItems.Count + (e.NewValue == CheckState.Checked ? 1 : -1);

            string text, fullText;

            if (checkedCount == 0)
            {
                text = fullText = string.Empty;
            }
            else if (checkedCount == _checkedListBox.Items.Count)
            {
                text = fullText = "All";
            }
            else
            {
                var selectedItems = new List<string>(checkedCount);
                var items = _checkedListBox.Items.Cast<IShortNameable>().ToList();

                if (e == null)
                {
                    foreach (int index in _checkedListBox.CheckedIndices)
                    {
                        selectedItems.Add(items[index].ToStringShort());
                    }
                }
                else
                {
                    foreach (int index in _checkedListBox.CheckedIndices)
                    {
                        if (index != e.Index)
                            selectedItems.Add(items[index].ToStringShort());
                    }
                    if (e.NewValue == CheckState.Checked)
                        selectedItems.Add(items[e.Index].ToStringShort());
                }

                selectedItems.Sort();
                fullText = string.Join(", ", selectedItems);
                text = TextHelpers.TruncateWithEllipsis(fullText, _textBox.Font, _textBox.Width - 10);
            }

            //RR!!
            _textBox.Text = text;
            _toolTip.SetToolTip(_textBox, fullText);
            UpdateButtonStates(checkedCount);
        }

        private void UpdateButtonStates(int checkedCount)
        {
            _btnClear.Enabled = checkedCount > 0;
            _btnSelectAll.Enabled = checkedCount < _checkedListBox.Items.Count;
        }

        public static class TextHelpers
        {
            public static string TruncateWithEllipsis(string text, Font font, int maxWidth)
            {
                if (TextRenderer.MeasureText(text, font).Width <= maxWidth)
                    return text;

                var ellipsis = "...";
                int ellipsisWidth = TextRenderer.MeasureText(ellipsis, font).Width;

                for (int i = text.Length - 1; i > 0; i--)
                {
                    string truncated = text.Substring(0, i) + ellipsis;
                    if (TextRenderer.MeasureText(truncated, font).Width <= maxWidth)
                        return truncated;
                }
                return ellipsis;
            }
        }
    }// class CheckedComboBox
}// namespace Urenlijsten_App