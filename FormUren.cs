using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Windows.Forms;

using System;
using System.Linq;

namespace Urenlijsten_App
{
    internal class FormUren : Form
    {
        public FormUren()
        {
            Initialize(); // Jouw eigen methode
        }

        private void Initialize()
        {
            this.Text = "Urenlijst";
            this.Size = new Size(400, 600); // Startgrootte

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                AutoSize = true
            };

            // Zorg ervoor dat de eerste kolom alles opvult
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Voeg rijen toe aan de tabel
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // Logo-Naam
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));  // Spacer-1
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 60)); // Uren (grootste deel)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));  // Spacer-2
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 30)); // Verlof
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));  // Spacer-3
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Submit (vaste hoogte)

            // Panels maken
            var panelLogoNaam = CreatePanel(Color.LightBlue, "Logo-Naam");
            var panelUren = CreatePanel(Color.LightGreen, "Uren");
            var panelVerlof = CreatePanel(Color.LightCoral, "Verlof");
            var panelSubmit = CreatePanel(Color.Gray, "Submit");
            //panelSubmit.Height = 40;

            // Spacers (lege panelen met vaste hoogte)
            var spacer1 = CreateSpacer();
            var spacer2 = CreateSpacer();
            var spacer3 = CreateSpacer();

            var btnSumbit = new Button()
            {
                Text = "Submit",
                Width = 100,
                Dock = DockStyle.Right,
            };
            panelSubmit.Controls.Add(btnSumbit);
            btnSumbit.Click += BtnSubmit_Click;

            // Toevoegen aan de tabel
            table.Controls.Add(panelLogoNaam, 0, 0);
            table.Controls.Add(spacer1, 0, 1);
            table.Controls.Add(panelUren, 0, 2);
            table.Controls.Add(spacer2, 0, 3);
            table.Controls.Add(panelVerlof, 0, 4);
            table.Controls.Add(spacer3, 0, 5);
            table.Controls.Add(panelSubmit, 0, 6);

            this.Controls.Add(table);
        }

        private Panel CreatePanel(Color color, string text)
        {
            return new Panel
            {
                BackColor = color,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Controls = { new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter } }
            };
        }

        private Panel CreateSpacer()
        {
            return new Panel
            {
                BackColor = Color.Transparent, // Onzichtbare spacer
                Dock = DockStyle.Fill
            };
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Formulier verzonden!", "Bevestiging", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

    }
}
