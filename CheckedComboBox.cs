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

    public class CheckedComboBox : UserControl
    {
        // Child controls
        private TableLayoutPanel _layoutPanel;
        private TextBox _textBox;
        private Button _dropdownButton;
        private CheckedListBox _checkedListBox;
        private Panel _dropdownPanel;
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
            _layoutPanel = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                ColumnStyles =
                {
                    new ColumnStyle(SizeType.AutoSize), // Button
                    new ColumnStyle(SizeType.Percent, 100F) // TextBox
                },
                RowStyles =
                {
                    new RowStyle(SizeType.AutoSize) // Or SizeType.Absolute with desired height
                },
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle
            };

            _textBox = new TextBox()
            {
                Dock = DockStyle.Fill // Or set Anchor to Left, Right
            };
            _layoutPanel.Controls.Add(_textBox, 0, 0);

            _dropdownButton = new Button
            {
                Text = "▼",
                Dock = DockStyle.Right,
                FlatStyle = FlatStyle.Flat,
            };
            _layoutPanel.Controls.Add(_dropdownButton, 1, 0);
            this.Controls.Add(_layoutPanel);

            _dropdownPanel = new Panel
            {
                Dock = DockStyle.None,
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Magenta

            };
            _checkedListBox = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                HorizontalScrollbar = true,
            };
            _dropdownPanel.Controls.Add(_checkedListBox);

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
            _dropdownButton.Click += (s, e) => ToggleDropdown();

            // Focus handling
            _textBox.GotFocus += (s, e) => OnGotFocus(e);
            _dropdownButton.GotFocus += (s, e) => OnGotFocus(e);
            this.LostFocus += OnThisLostFocus;

            // Checkbox changes
            _checkedListBox.ItemCheck += (s, e) => UpdateTextDisplay();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            _textBox.BackColor = SystemColors.Highlight;
            base.OnGotFocus(e);
        }

        private void OnThisLostFocus(object? sender, EventArgs e)
        {
            if (!_dropdownPanel.Bounds.Contains(PointToClient(MousePosition)))
                CloseDropdown();
            _textBox.BackColor = SystemColors.Window;
        }

        private void ToggleDropdown()
        {
            if (_dropdownPanel.Visible)
                CloseDropdown();
            else
                OpenDropdown();
        }

        private void OpenDropdown()
        {
            PositionDropdown();
            _dropdownPanel.Show();
            _dropdownPanel.BringToFront();
        }

        private void CloseDropdown()
        {
            _dropdownPanel.Hide();
        }

        private void PositionDropdown()
        {
            // Ensure the dropdown panel is added to the ParentForm's controls
            if (ParentForm != null && !ParentForm.Controls.Contains(_dropdownPanel))
            {
               ParentForm.Controls.Add(_dropdownPanel);
            }

            // Determine the position of the top-left corner of your custom control on the screen
            var controlScreenPos = PointToScreen(Point.Empty);

            // Calculate the position of the bottom-left corner of your custom control on the screen
            var controlScreenPosBottom = PointToScreen(new Point(0, Height));

            // Convert screen coordinates back to form coordinates
            var controlFormPos = ParentForm.PointToClient(controlScreenPos);
            var controlFormPosBottom = ParentForm.PointToClient(controlScreenPosBottom);

            // Determine the available space below the control
            int spaceBelow = Screen.GetWorkingArea(controlScreenPosBottom).Bottom - controlScreenPosBottom.Y;

            _dropdownPanel.Width = Width;

            // Calculate the desired height based on the number of items
            int itemCount = _checkedListBox.Items.Count;
            int itemHeight = _checkedListBox.ItemHeight > 0 ? _checkedListBox.ItemHeight : 16; // Use the ItemHeight of the CheckedListBox or a default value
            int preferredHeight = itemCount * itemHeight + 2; // +2 for the borders

            // Set a maximum height (e.g., 200 pixels) to prevent the dropdown from becoming too large
            const int maxHeight = 200;
            _dropdownPanel.Height = Math.Max(preferredHeight, 20);
            _dropdownPanel.Height = Math.Min(preferredHeight, maxHeight);

            if (spaceBelow > _dropdownPanel.Height)
            {
            // Open downward: place the top-left corner of the dropdown panel
            // directly below the bottom-left corner of the custom control
            _dropdownPanel.Location = controlFormPosBottom;
            }
            else
            {
            // Open upward: place the bottom-left corner of the dropdown panel
            // directly above the top-left corner of the custom control
            _dropdownPanel.Location = new Point(controlFormPos.X, controlFormPos.Y - _dropdownPanel.Height);
            }

            // Zet location op 0,0 voor debugging
            //_dropdownPanel.Location = Point.Empty;
        }

        // ---- Text Display ----
        private void UpdateTextDisplay()
        {
            var selected = GetCheckedValuesShort();

            string fullText = string.Join(", ", selected);
            _textBox.Text = TextHelpers.TruncateWithEllipsis(fullText, _textBox.Font, _textBox.Width - 10);

            // Set ToolTip text (shows full text on hover)
            _toolTip.SetToolTip(_textBox, fullText);
        }
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
}// namespace CustomControls