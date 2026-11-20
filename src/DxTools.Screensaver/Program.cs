
namespace DxTools.Screensaver;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var application = new System.Windows.Application
        {
            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown,
        };

        var controller = new ScreensaverController(application);
        application.Startup += (_, _) => controller.Start(args);
        application.Run();
    }
}
