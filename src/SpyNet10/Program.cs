namespace SpyNet10;

internal static class Program
{
    [STAThread]
    private static int Main()
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