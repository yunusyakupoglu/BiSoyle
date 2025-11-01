namespace BiSoyleAdminGUI;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }    
}