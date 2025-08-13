using System;
using System.Windows.Forms;

namespace CzatujiemyClient
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ShowStartupWarning();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            var loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                var mainForm = new MainForm(loginForm.Nick, loginForm.Password, 
                                          loginForm.ServerIP, loginForm.ServerPort, loginForm.IsRegister);
                Application.Run(mainForm);
            }
        }
        private static void ShowStartupWarning()
        {
            string wiadomosc = @"⚠️ WAŻNE OSTRZEŻENIE ⚠️

Ta aplikacja zawiera BARDZO DUŻO BŁĘDÓW i prawdopodobnie NIGDY nie otrzyma aktualizacji z mojej strony (jestem zbyt leniwy).

📅 SERWER PUBLICZNY (9.223.113.121:8080) PRZESTANIE DZIAŁAĆ 20 WRZEŚNIA 2025

🔧 Kod źródłowy jest dostępny na GitHub – jeśli chcesz, możesz stworzyć własną wersję.

❗ Uwaga: wszystkie wiadomości są wysyłane do serwera w formie tekstowej. 
Ten klient nie jest bezpieczny – każda wiadomość jest zapisywana na serwerze jako zwykły tekst. 
Każdy, kto ma dostęp do tego serwera, może przeczytać Twoje wiadomości. 

💡 W skrócie: korzystaj z tego klienta tylko na publicznym serwerze lub na takim, który sam hostujesz i któremu ufasz.

Czy na pewno chcesz kontynuować?";


            var wynik = MessageBox.Show(wiadomosc, "Czatujemy 2.0 - Ostrzeżenie", 
                                      MessageBoxButtons.YesNo, 
                                      MessageBoxIcon.Warning,
                                      MessageBoxDefaultButton.Button2);
            
            if (wynik == DialogResult.No)
            {
                Environment.Exit(0);
            }
        }
    }
}