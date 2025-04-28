using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urenlijsten_App
{
    class Test
    {
        private ComboBox _comboBox;

        // Constructor van de klasse Test
        public Test()
        {
            // Initialiseer de _comboBox member in de constructor.
            // Gebruik object-initializer syntax {} om eigenschappen direct in te stellen.
            _comboBox = new ComboBox
            {
                // Eigenschappen instellen met object-initializer
                DropDownStyle = ComboBoxStyle.DropDown,   // User can enter values in the TextBox area.
                AutoCompleteMode = AutoCompleteMode.None,
                AutoCompleteSource = AutoCompleteSource.None,
                DataSource = new List<string> { "11", "12", "13", "21", "22", "23" },
               
            };


            // Let op: De Text eigenschap kan soms overschreven worden nadat DataSource is ingesteld.
            // Stel deze dus eventueel hierna in als je een specifieke starttekst wilt zien.
            _comboBox.Text = "Blah";
        }

        // Optioneel: Een publieke property om toegang te geven tot de geconfigureerde ComboBox
        // (Naam aangepast van 'TestComboBox' om verwarring met de klasse/constructor te voorkomen)
        public ComboBox GetComboBox
        {
            get { return _comboBox; }
        }

        // Andere methoden, event handlers, etc. voor de Test klasse komen hier.
    }
}
