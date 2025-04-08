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
        private readonly TextBox _textBox = new() { ReadOnly = true };
        private readonly Button _dropdownButton = new() { Text = "▼" };
        private readonly CheckedListBox _checkedListBox = new();
        private readonly Panel _dropdownPanel = new() { Visible = false, BorderStyle = BorderStyle.FixedSingle };
        private readonly ToolTip _toolTip = new(); // Added ToolTip component

        // Data
        private Func<object, string> _shortNameSelector = x => x.ToString()!;

        public CheckedComboBox()
        {
            InitializeComponents();
            WireEvents();
        }

        // Public API
        public void SetDataSource<T>(List<T> items) where T : IShortNameable
        {
            _checkedListBox.DataSource = items;
            _shortNameSelector = x => ((IShortNameable)x).ToStringShort();
            UpdateTextDisplay();
        }

        // Initialization
        private void InitializeComponents()
        {
            // Layout setup (Dock, Size, etc.)
            _textBox.Dock = DockStyle.Fill;
            _dropdownButton.Dock = DockStyle.Right;
            _dropdownPanel.Size = new Size(Width, 150);

            // Hierarchy
            Controls.Add(_textBox);
            Controls.Add(_dropdownButton);
            Controls.Add(_dropdownPanel);
            _dropdownPanel.Controls.Add(_checkedListBox);

            // ToolTip setup
            _toolTip.AutoPopDelay = 5000;
            _toolTip.InitialDelay = 500;
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

        private void OnGotFocus(EventArgs e)
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
            var screenPos = PointToScreen(new Point(0, Height));
            int spaceBelow = Screen.GetWorkingArea(screenPos).Bottom - screenPos.Y;

            _dropdownPanel.Width = Width;
            _dropdownPanel.Height = Math.Min(150, spaceBelow - 5); // Now using spaceBelow
            _dropdownPanel.Location = spaceBelow > _dropdownPanel.Height
                ? new Point(0, Height)  // Open downward
                : new Point(0, -_dropdownPanel.Height); // Open upward
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