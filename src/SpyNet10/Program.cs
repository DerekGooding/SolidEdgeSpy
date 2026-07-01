namespace SpyNet10;

static class Program
{
    [STAThread]
    static int Main()
    {
        try
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new MainForm());
        }
        catch
        {
            GlobalExceptionHandler.HandleException();
        }

        return 0;
    }
}
