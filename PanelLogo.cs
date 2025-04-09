using CustomControls;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using DocumentFormat.OpenXml.VariantTypes;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel.DataAnnotations;

namespace CustomControls
{
    public class PanelLogo : Panel
    {
        // Constants
        private const int panelHeight = 130;
        private const int spacerWidth = 5;
        private int hLayoutColumnCount = 0;

        private DateTime _fromDate; // maandag
        private DateTime _toDate;   // vrijdag
        private bool validateOK = false;

        // Public widget declarations
        public TableLayoutPanel hLayout;
        public PictureBox picLogo;
        public Panel panelCompany;
        public Label lblCompany, lblAddress, lblCity, lblPhone;
        public GroupBox grpBedrijf;
        public RadioButton rdoLogistics, rdoInternational;
        public TableLayoutPanel tableNameWeek;
        public Label lblName, lblWeekNr, lblVan, lblTot, lblTotDate;
        public TextBox txtName, txtWeek;
        public CustomDateControl ctrlWeek;

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                _fromDate = value.DayOfWeek switch
                {
                    DayOfWeek.Monday => value.Date,
                    DayOfWeek.Sunday => value.AddDays(-6).Date,
                    _ => value.AddDays(-(int)value.DayOfWeek + 1).Date
                };
                ctrlWeek.Date = _fromDate.ToString("dd/MM/yyyy");

                _toDate = _fromDate.AddDays(4);
                lblTotDate.Text = _toDate.ToString("dd/MM/yyyy");
                validateOK = true;

                // ISO 8601
                // Eerste dag van de week: maandag
                // Eerste week met ≥4 dagen in het nieuwe jaar
                int isoWeek1 = ISOWeek.GetWeekOfYear(_fromDate);
                txtWeek.Text = isoWeek1.ToString();
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
        }

        public PanelLogo()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Panel setup
            this.Height = panelHeight;
            this.Dock = DockStyle.Top;
            this.BackColor = SystemColors.Control; // Standaard grijze achtergrondkleur
            this.BorderStyle = BorderStyle.FixedSingle;

            // Main horizontal layout table
            hLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 1,
                ColumnStyles =
                {
                    new ColumnStyle(SizeType.AutoSize), // Image
                    new ColumnStyle(SizeType.Absolute, spacerWidth), // Spacer
                    new ColumnStyle(SizeType.AutoSize),              // Company
                    new ColumnStyle(SizeType.Absolute, spacerWidth), // Spacer
                    new ColumnStyle(SizeType.Absolute, 160F),        // Radio group
                    new ColumnStyle(SizeType.Percent, 100F),         // Stretch
                    new ColumnStyle(SizeType.AutoSize)               // panelNameWeek
                },
                Padding = new Padding(0)
            };
            this.Controls.Add(hLayout);

            // Column 0 - Image with proper aspect ratio
            try
            {
                string path = System.IO.Path.Combine(Application.StartupPath, @"..\..\..\icons\bocari.jpeg");
                path = Path.GetFullPath(path);  // normalized path without \..\
                picLogo = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.Zoom, // Maintain aspect ratio
                    Image = Image.FromFile(path),
                    Height = panelHeight,               // Only set height
                    Dock = DockStyle.Fill
                };

                // Update the column width to match scaled image
                hLayout.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, picLogo.Width);
                AddColumnToHLayout(picLogo);
            }
            catch (Exception)
            {
                AddColumnToHLayout(new Label { Text = "Bocari Logo", Dock = DockStyle.Fill });
            }

            AddSpacerToHLayout();

            // Column 2 - Company info
            panelCompany = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = panelHeight,
                //Margin = new Padding(10),
            };

            lblCompany = new Label
            {
                Text = "BoCari Logistic Services B.V.",
                Dock = DockStyle.Top,
                Font = new Font(DefaultFont, FontStyle.Bold)
            };

            lblAddress = new Label { Text = "Ettensebaan 25B", Dock = DockStyle.Top };
            lblCity = new Label { Text = "4813 AH Breda", Dock = DockStyle.Top };
            lblPhone = new Label { Text = "Tel: 088-0049200", Dock = DockStyle.Top };

            panelCompany.Controls.Add(lblPhone);
            panelCompany.Controls.Add(lblCity);
            panelCompany.Controls.Add(lblAddress);
            panelCompany.Controls.Add(lblCompany);
            AddColumnToHLayout(panelCompany);

            AddSpacerToHLayout();

            grpBedrijf = new GroupBox
            {
                Text = "Bedrijf",
                AutoSize = false,     // Laat de GroupBox zelf zijn hoogte bepalen
                Dock = DockStyle.Top, // Gebruik Top i.p.v. Fill
                Padding = new Padding(5),
                Height = 80,
                //Width = 250,
                ClientSize = new Size(250,80)
            };

            rdoLogistics = new RadioButton
            {
                Text = "BoCari Logistic Services B.V.",
                Dock = DockStyle.Top,
                Checked = true,
                Margin = new Padding(0, 5, 0, 5)
            };
            rdoLogistics.CheckedChanged += RadioButton_CheckedChanged;

            rdoInternational = new RadioButton
            {
                Text = "Bocari International",
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 5)
            };
            rdoInternational.CheckedChanged += RadioButton_CheckedChanged;

            grpBedrijf.Controls.AddRange(new Control[] { rdoInternational, rdoLogistics });
            AddColumnToHLayout(grpBedrijf);


            AddSpacerToHLayout();

            // Column 6 - Name/Week table
            tableNameWeek = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 5,
                AutoSize = true,
                Dock = DockStyle.Fill,
                ColumnStyles = // Breedtes
                {
                    new ColumnStyle(SizeType.Absolute, 60),  // Labels
                    new ColumnStyle(SizeType.AutoSize),
                    new ColumnStyle(SizeType.Percent, 100F), // Spacer
                },
            };

            // Initialiseer alle rijstijlen eerst (hoogtes).
            tableNameWeek.RowStyles.Clear();
            tableNameWeek.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableNameWeek.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableNameWeek.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableNameWeek.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableNameWeek.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Extra rij voor restruimte

            // Row 0
            lblName = new Label
            {
                Text = "Naam",
                TextAlign = ContentAlignment.MiddleRight,

            };
            txtName = new TextBox
            {
                //Dock = DockStyle.Fill,
                AutoSize = true,
                Width = 160,
                Padding = new Padding(0, 0, 15, 0), // Left, Top, Right, Bottom margin (5 pixels on the right)
            };
            tableNameWeek.Controls.Add(lblName, 0, 0);
            tableNameWeek.Controls.Add(txtName, 1, 0);

            // Row 1
            lblWeekNr = new Label
            {
                Text = "Week nr.",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill
            };
            txtWeek = new TextBox
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
            };
            tableNameWeek.Controls.Add(lblWeekNr, 0, 1);
            tableNameWeek.Controls.Add(txtWeek, 1, 1);
            // Voeg de nieuwe event handlers toe voor txtWeek
            txtWeek.KeyPress += txtWeek_KeyPress;
            txtWeek.Leave += (sender, e) => ValidateWeekHelper();

            // Row 2
            lblVan = new Label
            {
                Text = "van",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill
            };
            ctrlWeek = new CustomDateControl(CustomDateControl.DateType.Full)
            {
                //Height = 40 // CustomDateControl.PreferredHeight
                Dock = DockStyle.Fill,
                AutoSize = true,
            };
            tableNameWeek.Controls.Add(lblVan, 0, 2);
            tableNameWeek.Controls.Add(ctrlWeek, 1, 2);

            // Row 3
            lblTot = new Label
            {
                Text = "tot",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill
            };
            lblTotDate = new Label
            {
                Text = "", // The date value
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            tableNameWeek.Controls.Add(lblTot, 0, 3);
            tableNameWeek.Controls.Add(lblTotDate, 1, 3);

            var panelNameWeek = new Panel()
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Height = panelHeight - 50,
                BackColor = SystemColors.Control, // Standaard grijze achtergrondkleur
                Margin = new Padding(10),
                //BorderStyle = BorderStyle.FixedSingle
            };
            panelNameWeek.Controls.Add(tableNameWeek);
            AddColumnToHLayout(panelNameWeek);

            this.PerformLayout();
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoLogistics.Checked == true)
            {
                lblCompany.Text = "Logistics";
            }
            else if (rdoInternational.Checked == true)
            {
                lblCompany.Text = "International";
            }
        }

        private void txtWeek_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Enter afhandeling
            if (e.KeyChar == (char)Keys.Enter)
            {
                ValidateWeekHelper();
                e.Handled = false;
                return;
            }

            // Sta cijfers, backspace en tab toe
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b' && e.KeyChar != '\t')
            {
                e.Handled = true;
                return;
            }

            // Maximaal 2 cijfers
            if (char.IsDigit(e.KeyChar) && ((TextBox)sender).Text.Length >= 2)
            {
                validateOK = false;
                e.Handled = true;
                return;
            }

            // Alleen bij geldige invoer terug naar wit
            txtWeek.BackColor = Color.White;
        }

        private void ValidateWeekHelper()
        {
            validateOK = ValidateWeek();
            if (!validateOK)
            {
                txtWeek.BackColor = Color.LightCoral;
            }
        }

        private bool ValidateWeek()
        {
            if (!int.TryParse(txtWeek.Text, out int weekNumber) || weekNumber < 1 || weekNumber > 53)
                return false;

            try
            {
                int year = DateTime.Now.Year;

                // Controleer of het weeknummer geldig is voor het huidige jaar
                if (weekNumber > ISOWeek.GetWeeksInYear(year))
                    return false;

                // Bepaal de maandag van de opgegeven week
                DateTime mondayOfWeek = ISOWeek.ToDateTime(year, weekNumber, DayOfWeek.Monday);

                // Controleer of de berekende datum in het huidige jaar valt
                if (mondayOfWeek.Year != year && weekNumber != 1)
                    return false;

                // Stel FromDate in (wat automatisch ToDate en labels update)
                FromDate = mondayOfWeek;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void AddColumnToHLayout(Control widget)
        {
            // Zet de margin van het widget op 0
            widget.Margin = new Padding(0);

            // Voeg het widget toe aan de nieuwe kolom
            hLayout.Controls.Add(widget, hLayoutColumnCount, 0);
            hLayoutColumnCount++;
        }

        private void AddSpacerToHLayout()
        {
            AddColumnToHLayout(new Panel // spacer
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Bisque
            });
        }
    }// class PanelLogo
}