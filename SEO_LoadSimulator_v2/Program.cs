using System;
using System.Windows.Forms;

namespace SEO_LoadSimulator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var db = AppDb.Default();
            using (var login = new LoginForm(db))
            {
                if (login.ShowDialog() != DialogResult.OK || login.User == null) return;
                Application.Run(new MainForm(login.User, db));
            }
        }
    }
}
