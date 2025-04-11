using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

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

        // Constructor
        public CheckedComboBox()
        {
            InitializeComponents();
            WireEvents();
        }

        // Public API
        public void SetDataSource<T>(List<T> items) where T : IShortNameable
        {
            _checkedListBox.DataSource = items;
            UpdateTextDisplay();
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
            this.Controls.Add(_layoutPanel);

            // Clear All Button (◻)
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
            _btnClear.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            _btnClear.Click += (s, e) => ClearSelections();
            _layoutPanel.Controls.Add(_btnClear, 2, 0);

            // Select All Button (◼)
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
            _btnSelectAll.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            _btnSelectAll.FlatAppearance.BorderSize = 0;
            _btnSelectAll.Click += (s, e) => SelectAll();
            _layoutPanel.Controls.Add(_btnSelectAll, 3, 0);

            // Add tooltips
            _toolTip.SetToolTip(_btnClear, "Clear All");
            _toolTip.SetToolTip(_btnSelectAll, "Select All");

            // ToolTip setup
            _toolTip.AutoPopDelay = 5000;
            _toolTip.InitialDelay = 500;
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

        // ---- Focus & Dropdown Management ----
        private void WireEvents()
        {
            // Click handling
            _textBox.Click += (s, e) => ToggleDropdown();
            _btnDropDown.Click += (s, e) => ToggleDropdown();

            // Focus handling
            _textBox.GotFocus += (s, e) => OnGotFocus(e);
            _btnDropDown.GotFocus += (s, e) => OnGotFocus(e);
            this.LostFocus += OnThisLostFocus;

            // Checkbox changes
            _checkedListBox.ItemCheck += (s, e) => UpdateTextDisplay(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            //_textBox.BackColor = SystemColors.Highlight;
            base.OnGotFocus(e);
        }

        /*
        private void OnThisLostFocus(object? sender, EventArgs e)
        {
            if (!_panelDropDown.Bounds.Contains(PointToClient(MousePosition)))
                CloseDropdown();
            _textBox.BackColor = SystemColors.Window;
        }
        */

        private void OnThisLostFocus(object? sender, EventArgs e)
        {
            // Als muis zich links of rechts van de control bevindt, sluit dropdown
            var mousePos = PointToClient(Control.MousePosition);
            if (mousePos.X < 0 || mousePos.X > this.Width)
            {
                CloseDropdown();
            }

            _textBox.BackColor = SystemColors.Window;
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
            PositionDropdown();
            _panelDropDown.Show();
            _panelDropDown.BringToFront();
        }

        private void CloseDropdown()
        {
            _panelDropDown.Hide();
        }

        private void PositionDropdown()
        {
            // Ensure the dropdown panel is added to the ParentForm's controls
            if (ParentForm != null && !ParentForm.Controls.Contains(_panelDropDown))
            {
                ParentForm.Controls.Add(_panelDropDown);
            }

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

                fullText = string.Join(", ", selectedItems);
                text = TextHelpers.TruncateWithEllipsis(fullText, _textBox.Font, _textBox.Width - 10);
            }

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
}// namespace CustomControls