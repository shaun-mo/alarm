using System.Threading;
using System.Windows;

namespace SunriseAlarm.App;

public partial class App : Application
{
    private static Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single instance: if the wake task launches a second copy while one is already
        // running and armed, let the existing one own the alarm and exit this one.
        _singleInstance = new Mutex(initiallyOwned: true, @"Local\SunriseAlarm.SingleInstance", out bool isNew);
        if (!isNew)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }
}
