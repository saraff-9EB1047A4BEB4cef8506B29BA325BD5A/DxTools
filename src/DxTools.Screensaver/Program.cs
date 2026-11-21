
namespace DxTools.Screensaver;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        DxTools.Core.DxLog.Initialize();

        var application = new System.Windows.Application
        {
            ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown,
        };

        var controller = new ScreensaverController(application);
        application.Startup += (_, _) => controller.Start(args);
        application.Run();
    }
}
