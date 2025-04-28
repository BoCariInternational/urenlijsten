using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Urenlijsten_App
{
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using System.ComponentModel;

    namespace CustomControls
    {
        // Basisklasse voor een custom editing control in een DataGridView, gebaseerd op UserControl.
        // Dit partial deel implementeert de IDataGridViewEditingControl interface.
        // TDerived: De concrete klasse die hiervan erft (voor CRTP).
        public abstract partial class DataGridViewUserControl<TDerived> : UserControl,
            IDataGridViewEditingControl
            where TDerived : DataGridViewUserControl<TDerived> // Constraint voor CRTP
        {
            // =============================================
            // Intern Control Lid (nu van het type Control - noodzakelijk voor interface-implementatie in DIT partial deel)
            // =============================================
            // Dit lid MOET in DIT partial deel gedeclareerd zijn om de IDataGridViewEditingControl methoden
            // hier te kunnen implementeren door naar _control te verwijzen.
            // De afgeleide klasse OF een ander partial deel kan dit lid instantieren en toewijzen.
            protected Control _control;

            // =============================================
            // Private fields voor IDataGridViewEditingControl
            // =============================================
            private DataGridView _internalDataGridView;
            private bool _valueChanged;
            private int _rowIndex;

            // De constructor hoort meestal in één deel van de partial class;
            // als je hem in een ander partial deel wilt, haal deze hier dan weg.
            // public DataGridViewUserControl()
            // {
            //    // Basis configuratie UserControl, _control wordt elders geïnstantieerd.
            //    this.AutoSize = true;
            //    this.Margin = new Padding(0);
            // }


            // =============================================
            // IDataGridViewEditingControl Interface Members (nu met volledige method bodies)
            // =============================================

            // 1. Core Properties
            public DataGridView EditingControlDataGridView
            {
                get { return _internalDataGridView; }
                set { _internalDataGridView = value; }
            }

            public int EditingControlRowIndex
            {
                get { return _rowIndex; }
                set { _rowIndex = value; }
            }

            public bool EditingControlValueChanged
            {
                get { return _valueChanged; }
                // De setter wordt door het grid gebruikt om de vlag te resetten
                set { _valueChanged = value; }
            }

            // 2. Value Management
            // Deze property MOET abstract zijn omdat de manier om de "waarde" te krijgen/zetten
            // afhangt van het specifieke type _control (TextBox.Text, ComboBox.SelectedItem, etc.).
            public abstract object EditingControlFormattedValue { get; set; }

            // Deze methode gebruikt de abstracte property.
            public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
            {
                // Retourneer de opgemaakte waarde via de abstracte property.
                // Afgeleide klassen moeten implementeren hoe EditingControlFormattedValue werkt
                // voor hun specifieke interne control.
                return EditingControlFormattedValue;
            }

            // 3. Visual Styling
            public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
            {
                // Pas de stijl van de cel toe op de interne control _control
                // en eventueel op de UserControl zelf (bijv. BackColor).
                // We gaan ervan uit dat _control hier al geïnstantieerd is.
                if (_control != null)
                {
                    _control.Font = dataGridViewCellStyle.Font;
                    _control.ForeColor = dataGridViewCellStyle.ForeColor;
                    _control.BackColor = SystemColors.Window; // Vaak SystemColors.Window voor editing controls
                                                              // of dataGridViewCellStyle.BackColor als je die wilt overnemen
                }
                this.BackColor = dataGridViewCellStyle.BackColor; // Achtergrond van de UserControl zelf
                _control.KeyDown += OnKeyDown(object sender, KeyEventArgs e);
            }

            [Browsable(false)] // Verberg in designer
            [EditorBrowsable(EditorBrowsableState.Advanced)] // Verberg in IntelliSense
            public Cursor EditingPanelCursor
            {
                get { return Cursors.Default; } // Kan overschreven worden indien nodig
            }

            // 4. Behavior Control
            public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
            {
                // Deze methode bepaalt of de interne control (_control) de toetsaanslag wil afhandelen
                // of dat het grid (of de UserControl) dit moet doen.
                // Dit is een generieke implementatie; afgeleide klassen kunnen dit verfijnen.

                // We gaan ervan uit dat _control hier al geïnstantieerd is.
                if (_control == null) return false; // Geen control? Laat grid het afhandelen.

                Keys keyCode = keyData & Keys.KeyCode; // Haal alleen de KeyCode op, zonder modifiers

                // Stuur specifieke navigatie- en bewerkingstoetsen naar de interne Control
                // (die ze op zijn beurt afhandelt, bijv. TextBox voor tekst, ComboBox voor dropdown/nav)
                switch (keyCode)
                {
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Home:
                    case Keys.End:
                    case Keys.PageUp:
                    case Keys.PageDown:
                    case Keys.Delete:
                    case Keys.Back:
                        return true; // Altijd doorgeven aan de control voor standaard bewerking/navigatie

                    case Keys.Enter: // ENTER wil je vaak zelf afhandelen (selectie bevestigen)
                    case Keys.Escape: // ESCAPE wil je vaak zelf afhandelen (bewerking annuleren)
                    case Keys.Tab: // TAB wil je vaak zelf afhandelen (naar volgende cel)
                        return !dataGridViewWantsInputKey; // Geef door aan de control TENZIJ grid het absoluut wil

                    default:
                        // Voor alle andere toetsen (letters, cijfers, symbolen, spatie, etc.):
                        // Geef ze door aan de control TENZIJ het grid ze echt zelf wil hebben.
                        // De standaard ComboBox handling in EditingControlShowing is wat we wilden vermijden.
                        // Hier zeggen we: stuur TYPISCHE input toetsen ALTIJD naar de control.
                        if (Char.IsControl((char)keyCode) || Char.IsLetterOrDigit((char)keyCode) || Char.IsPunctuation((char)keyCode) || Char.IsSeparator((char)keyCode) || keyCode == Keys.Space)
                        {
                            return true;
                        }
                        // Voor overige toetsen, volg het advies van het grid
                        return !dataGridViewWantsInputKey;
                }
                // Opmerking: Afhandeling van bijv. Enter/Escape/Tab (en roepen EndEdit/CancelEdit)
                // gebeurt in KeyDown/KeyUp events VAN DE INTERNE CONTROL (_control) in de afgeleide klasse.
            }

            public void PrepareEditingControlForEdit(bool selectAll)
            {
                // Wordt aangeroepen wanneer de cel in bewerkingsmodus komt.
                // Zorg ervoor dat de interne control de focus krijgt.
                // Selectie (selectAll) is afhankelijk van het specifieke type _control
                // en moet in de afgeleide klasse worden geïmplementeerd.

                _control?.Focus(); // Zet de focus op de interne control indien aanwezig

                // Afgeleide klasse moet de selectie afhandelen als _control dit ondersteunt
                // en selectAll true is.
            }

            [Browsable(false)] // Verberg in designer
            [EditorBrowsable(EditorBrowsableState.Advanced)] // Verberg in IntelliSense
            public bool RepositionEditingControlOnValueChange
            {
                get { return false; } // Meestal false voor tekstuele controls
            }

            // =============================================
            // Event Handling voor de interne control (moet in afgeleide klas)
            // =============================================
            // Afgeleide klassen moeten events van _control abonneren
            // (bijv. TextChanged, KeyDown, SelectedIndexChanged)
            // en van daaruit:
            // 1. _valueChanged instellen op true wanneer nodig.
            // 2. this.EditingControlDataGridView?.NotifyCurrentCellDirty(true); aanroepen.
            // 3. this.EditingControlDataGridView?.EndEdit() of CancelEdit() aanroepen
            //    bij een definitieve actie (bijv. Enter, selectie).
            // 4. De OnLeave event van de UserControl afhandelen in de afgeleide klas
            //    om EndEdit/CancelEdit af te handelen bij focus loss (optioneel, wees voorzichtig).

            // Helpende methode om _valueChanged in te stellen en het grid te notificeren
            protected void SetValueChangedAndNotifyGrid()
            {
                _valueChanged = true;
                this.EditingControlDataGridView?.NotifyCurrentCellDirty(true);
                // Let op: dit triggert *nog niet* EndEdit! Dat moet jij zelf regelen.
            }

            private void OnKeyDown(object sender, KeyEventArgs e)
            {
                // Vang specifieke toetsen op om de bewerking te controleren.
                ComboBox internalComboBox = (ComboBox)_control;

                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        // Bij ENTER: Beschouw de bewerking als voltooid op basis van de huidige tekst.
                        // JIJ ROEPT NU ZELF EndEdit() AAN.
                        this.EditingControlDataGridView?.EndEdit();
                        // BELANGRIJK: Markeer de event als afgehandeld zodat het grid
                        // deze ENTER niet verder verwerkt (zoals naar de volgende cel springen).
                        e.Handled = true;
                        e.SuppressKeyPress = true; // Voorkom dat de toets 'klinkt' in de control

                        // TODO: Voeg hier eventueel validatie toe VOORDAT EndEdit wordt aangeroepen.
                        break;

                    case Keys.Escape:
                        // Bij ESCAPE: Annuleer de bewerking.
                        // JIJ ROEPT NU ZELF CancelEdit() AAN.
                        this.EditingControlDataGridView?.CancelEdit();
                        e.Handled = true;
                        e.SuppressKeyPress = true; // Voorkom 'klinken'

                        // We hoeven _valueChanged niet op true te zetten voor annuleren.
                        break;

                    case Keys.Tab:
                        // Bij TAB: Laat het grid de focus naar de volgende cel verplaatsen.
                        // Dit gedrag wil je meestal behouden. Handled=false in EditingControlWantsInputKey regelt dit.
                        // Hier doen we geen specifieke actie in KeyDown.
                        break;

                        // TODO: Voeg hier eventueel logica toe voor pijltoetsen als je een
                        // custom dropdown/lijst beheert, om navigatie daarin af te handelen.
                }
            }

        }
    }
}
