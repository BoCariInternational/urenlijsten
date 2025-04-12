namespace Urenlijsten_App
{
    internal static class Program 
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new FormUren());
            }
            catch (Exception ex)
            {
                // Log de fout of geef een foutmelding aan de gebruiker
                MessageBox.Show("Er is een fout opgetreden: " + ex.Message);
            }
        }
    }
}