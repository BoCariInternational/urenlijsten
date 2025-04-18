﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Urenlijsten_App;
//using CV_App3;

namespace CustomControls
{
    public class CalendarForm : Form
    {
        private MonthCalendar calendar;
        private Button btnOK;

        public DateTime SelectedDate => calendar.SelectionStart;

        public CalendarForm(DateTime? initialDate)
        {
            int buttonWidth = 100;
            int buttonHeight = 45;
            Width = 730;
            Height = 280;
            AutoSize = false;

            // Basis form instellingen
            //FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Selecteer een datum";
            MinimizeBox = false;
            MaximizeBox = false;

            // Hoofd layout container
            var vertLayoutManager = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,  // Calendar | Spacer | Button panel
                ColumnCount = 1,
                Padding = new Padding(5),
                AutoSize = true
            };

            // Rij configuratie
            vertLayoutManager.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            vertLayoutManager.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Calendar
            vertLayoutManager.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));   // Spacer
            vertLayoutManager.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));  // Buttons
            // Calendar
            calendar = new MonthCalendar
            {
                CalendarDimensions = new Size(3, 1),
                MaxSelectionCount = 1,
                //Dock = DockStyle.Fill,
                MinDate = CustomDateControl.MinDate,
                MaxDate = CustomDateControl.MaxDate.AddDays(-1),
                FirstDayOfWeek = Day.Monday,
                ShowToday = false,          // de datum
                ShowTodayCircle = false,
            };

            if (initialDate.HasValue)
                calendar.SetDate(initialDate.Value);

            // Spacer panel
            var spacer = new Panel { Height = 5 };

            // Button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 45,
                BorderStyle = BorderStyle.None
            };

            btnOK = new Button
            {
                AutoSize = false,
                Text = "OK",
                DialogResult = DialogResult.OK, // scheelt een event handler.
                Image = FormUren.imageOk,
                ImageAlign = ContentAlignment.MiddleLeft,
                Width = buttonWidth,
                Height = buttonHeight,
                Dock = DockStyle.Right,
                //Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            };


            // Event handlers
            btnOK.Click += (s, e) => this.Close();
            // Layout
            buttonPanel.Controls.Add(btnOK);

            // Add controls to mainLayout
            vertLayoutManager.Controls.Add(calendar, 0, 0);
            vertLayoutManager.Controls.Add(spacer, 0, 1);
            vertLayoutManager.Controls.Add(buttonPanel, 0, 2);

            Controls.Add(vertLayoutManager);
        }
    }// class CalendarForm


    public class CustomDateControl : UserControl
    {
        public bool isValidOrEmpty;
        public enum DateType { Full, MonthYear }
        public DateType Type { get; set; } = DateType.Full;

        public static DateTime MinDate { get; } = new DateTime(1950, 1, 1);
        public static DateTime MaxDate { get; } = new DateTime(2100, 1, 1); // Still ongoing

        public int PreferredHeight { get; set; } = 40; // Standaardwaarde

        // Houd rekening met de PreferredHeight in je layout
        public override Size GetPreferredSize(Size proposedSize)
        {
            var baseSize = base.GetPreferredSize(proposedSize);
            return new Size(baseSize.Width, PreferredHeight);
        }

        public string DateString
        {
            get => txtDate.Text;
            set
            {
                txtDate.Text = value;
                ValidateDate();
            }
        }

        public DateTime? InputDate
        {
            get
            {
                return ParseDate(txtDate.Text, Type);
            }
            set
            {
                txtDate.Text = value.ToStringNL();
            }
        }

        private TextBox txtDate;
        private Button btnCalendar;

        public CustomDateControl(DateType type)
        {
            this.Width = 700;
            this.Height = 32;
            this.AutoSize = true;
            this.Dock = DockStyle.Fill;

            Type = type;

            txtDate = new TextBox()
            {
                Height = 20,
                Width = 80,
                //Dock = DockStyle.Left,
                Anchor = AnchorStyles.Left,
                AutoSize = false,
                TabIndex = 0,
            };
            txtDate.Location = new Point(0, (this.Height - txtDate.Height) / 2);

            btnCalendar = new Button()
            {
                Height = 32,
                Width = 32,
                //Dock = DockStyle.Right,
                Anchor = AnchorStyles.Left,
                AutoSize = false,
                FlatStyle = FlatStyle.Flat,
                Image = FormUren.imageCalendar,
                Name = "btnCalendar",
                TabIndex = 1,
                Text = "",
                UseVisualStyleBackColor = true,
                FlatAppearance =
                {
                    BorderSize = 0, // Zet de BorderSize van de FlatAppearance op 0
                }
            };
            this.Height = 35;
            btnCalendar.Location = new Point(txtDate.Width + 3, 0);

            Controls.Add(btnCalendar);
            Controls.Add(txtDate);

            // Voeg event handlers toe.
            txtDate.KeyPress += txtDate_KeyPress;
            txtDate.Leave += txtDate_Leave;
            btnCalendar.Click += btnCalendar_Click;

            // Stel de layout in.
            //this.Padding = new Padding(3);
            //this.Margin = new Padding(3);
        }

        private void btnCalendar_Click(object sender, EventArgs e)
        {
            DateTime? initialDate = ParseDate(DateString, DateType.Full);
            if (!initialDate.HasValue)
                initialDate = DateTime.Now;

            using var dialog = new CalendarForm(initialDate)
            {
                Dock = DockStyle.Fill,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
            };

            // Stel de geselecteerde datum in de kalender in.
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                DateString = dialog.SelectedDate.ToStringNL();
            }
        }

        private DateTime? ParseDate(string dateString, DateType type)
        {
            if (string.IsNullOrWhiteSpace(dateString)) return null;

            string[] dateParts = dateString.Split(new char[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (dateParts == null || dateParts.Length < 2 || dateParts.Length > 3) // twee of drie groepen digits
            {
                return null;
            }

            int day = 1, month = -1, year = -1;

            // Case 1: dd-mm-yy(yy)
            if (dateParts.Length == 3 && type == DateType.Full)
            {
                if (!int.TryParse(dateParts[0], out day) || !int.TryParse(dateParts[1], out month) || !int.TryParse(dateParts[2], out year))
                {
                    return null;
                }
            }
            // Case 2: mm-yy(yy)
            else if (dateParts.Length == 2 && type == DateType.MonthYear)
            {
                if (!int.TryParse(dateParts[0], out month) || !int.TryParse(dateParts[1], out year))
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

            if (year < 100)
            {
                year += (year < 50) ? 2000 : 1900;
            }

            try
            {
                return new DateTime(year, month, day);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private bool ValidateDate()
        {
            isValidOrEmpty = true;

            if (string.IsNullOrWhiteSpace(txtDate.Text))
            {
                txtDate.Text = "";
                txtDate.BackColor = SystemColors.Window;             // Reset achtergrondkleur bij lege invoer
                OnDateChanged(new DateChangedEventArgs(null, true)); // Trigger event met null datum
                return isValidOrEmpty;
            }

            DateTime? parsedDate = ParseDate(txtDate.Text, this.Type);
            if (parsedDate.HasValue)
            {
                txtDate.Text = parsedDate.ToStringNL();
                txtDate.BackColor = SystemColors.Window; // Reset achtergrondkleur bij geldige datum
            }
            else
            {
                // MessageBox.Show("Ongeldige datum.", "Fout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtDate.BackColor = Color.LightCoral;
                isValidOrEmpty = false;
            }

            // Trigger het DateChanged event met de nieuwe datum
            OnDateChanged(new DateChangedEventArgs(parsedDate, isValidOrEmpty));
            return isValidOrEmpty;
        }

        private void txtDate_Leave(object sender, EventArgs e)
        {
            ValidateDate();
        }

        private void txtDate_KeyPress(object sender, KeyPressEventArgs e)
        {
            //RR!
            // Sta cijfers, spatie, streepje en slash toe.
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                (e.KeyChar != ' ') && (e.KeyChar != '-') && (e.KeyChar != '/'))
            {
                e.Handled = true; // Negeer het teken.
            }
            // Als Enter is ingedrukt, roep Validate aan en set handled = true
            else if (e.KeyChar == (char)Keys.Enter)
            {
                ValidateDate();    // Roep de Validate functie aan
                e.Handled = false; // Doe de standaard actie van de Enter toets
            }
        }

        // Custom event
        public class DateChangedEventArgs : EventArgs
        {
            public DateTime? SelectedDate { get; }
            public bool isValidOrEmpty { get; }           // false voor foute datum, true voor goede datum en whitespace/empty string
            public bool IsValidOrEmpty => isValidOrEmpty; // Alias voor isValidOrEmpty

            public DateChangedEventArgs(DateTime? selectedDate, bool isValidOrEmpty)
            {
                SelectedDate = selectedDate;
                this.isValidOrEmpty = isValidOrEmpty;
            }
        }

        public event EventHandler<DateChangedEventArgs> DateChanged;
        protected virtual void OnDateChanged(DateChangedEventArgs e)
        {
            DateChanged?.Invoke(this, e);
        }

    }// class CustomDateControl
}// namespace Urenlijsten_App.
