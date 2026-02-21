using AppKit;
using Foundation;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System;
using System.Timers;

namespace IR_SecondaryScreen;

[Register ("AppDelegate")]
public class AppDelegate : NSApplicationDelegate {
    
    private NSStatusItem _statusItem;
    private List<NSMenuItem> _irMenuItems = new List<NSMenuItem>();
    private NSMenuItem _statusMenuLabel;
    private Timer _statusTimer;
    private NSObject _activityToken; // Para evitar que la app se duerma

    private readonly string _host = "192.168.0.50";
    private readonly int _port = 4998;

    public override void DidFinishLaunching (NSNotification notification)
    {
        // 1. Prevenir que macOS duerma la app (Evita App Nap)
        _activityToken = NSProcessInfo.ProcessInfo.BeginActivity(
            NSActivityOptions.Background | NSActivityOptions.LatencyCritical, 
            "Mantenimiento de conexión IR"
        );

        _statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(NSStatusItemLength.Variable);
        _statusItem.Button.Title = "📺";

        var menu = new NSMenu();

        // Ítem de estado
        _statusMenuLabel = new NSMenuItem("Status: Connecting...");
        _statusMenuLabel.Enabled = false;
        menu.AddItem(_statusMenuLabel);
        menu.AddItem(NSMenuItem.SeparatorItem);

        // Definición de comandos 
        var optOnOff = new NSMenuItem("On / Off", async (s, e) => 
            await SendCommandAsync("sendir,2:1,1,39000,1,69,341,172,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,64,21,21,21,64,21,64,21,64,21,64,21,21,21,64,21,64,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,64,21,64,21,64,21,64,21,64,21,64,21,64,21,1612,341,86,21,3654\r\n"));
        
        var optVolUp = new NSMenuItem("   Volume Up", async (s, e) => 
            await SendCommandAsync("sendir,2:1,1,39000,1,69,341,172,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,64,20,20,20,64,20,64,20,64,20,64,20,20,20,64,20,20,20,20,20,64,20,64,20,20,20,20,20,20,20,20,20,64,20,64,20,20,20,20,20,64,20,64,20,64,20,64,20,1612,341,86,20,3654\r\n"));

        var optVolDown = new NSMenuItem("   Volume Down", async (s, e) => 
            await SendCommandAsync("sendir,2:1,1,39000,1,69,341,172,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,20,64,20,20,20,64,20,64,20,64,20,64,20,20,20,64,20,20,20,20,20,20,20,20,20,64,20,20,20,20,20,20,20,64,20,64,20,64,20,64,20,20,20,64,20,64,20,64,20,1612,341,86,20,3654\r\n"));

        var optMute = new NSMenuItem("   Mute / Unmute", async (s, e) => 
            await SendCommandAsync("sendir,2:1,1,39000,1,69,341,172,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,21,64,21,21,21,64,21,64,21,64,21,64,21,21,21,64,21,21,21,21,21,64,21,21,21,21,21,21,21,21,21,21,21,64,21,64,21,21,21,64,21,64,21,64,21,64,21,64,21,1612,341,86,21,3654\r\n"));

        _irMenuItems.AddRange(new[] { optOnOff, optVolUp, optVolDown, optMute });
        foreach (var item in _irMenuItems) menu.AddItem(item);

        menu.AddItem(NSMenuItem.SeparatorItem);
        
        menu.AddItem(new NSMenuItem("About...", (s, e) => {
            var alert = new NSAlert {
                MessageText =  NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleName").ToString(),
                InformativeText = $"Remote Control using a CG-100\nVersion: " + NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleShortVersionString").ToString() + "\n\nwith ❤️ Marteliuz Labs.\n2026",
                AlertStyle = NSAlertStyle.Informational
            };
            alert.AddButton("OK");
            alert.AddButton("Configure Device");
            if (alert.RunModal() == 1001) NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl($"http://{_host}"));
        }));

        menu.AddItem(new NSMenuItem("Exit", (s, e) => NSApplication.SharedApplication.Terminate(null)));
        _statusItem.Menu = menu;

        StartStatusTimer();
    }

    private void StartStatusTimer()
    {
        _statusTimer = new Timer(30000); // 30 segundos
        _statusTimer.Elapsed += async (s, e) => await CheckConnectionAsync();
        _statusTimer.AutoReset = true;
        _statusTimer.Enabled = true;

        // Ejecución inmediata al arrancar
        Task.Run(async () => await CheckConnectionAsync());
    }

    private async Task CheckConnectionAsync()
    {
        bool isAlive = false;
        try 
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(_host, _port, null, null);
            isAlive = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
            if (isAlive) client.EndConnect(result);
        }
        catch { isAlive = false; }

        InvokeOnMainThread(() => {
            _statusItem.Button.Title = isAlive ? "📺" : "📺";
            _statusMenuLabel.Title = isAlive ? $"Connected: {_host}" : "⚠️ CG-100 Disconnected";
            foreach (var item in _irMenuItems) item.Enabled = isAlive;
        });
    }

    private async Task SendCommandAsync(string irCode)
    {
        if (_statusItem.Button != null) _statusItem.Button.Title = "⚡️";
        var watch = System.Diagnostics.Stopwatch.StartNew();

        try 
        {
            await Task.Run(() => {
                using var client = new TcpClient();
                var result = client.BeginConnect(_host, _port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3))) throw new Exception();
                client.EndConnect(result);
                using var stream = client.GetStream();
                byte[] data = Encoding.ASCII.GetBytes(irCode);
                stream.Write(data, 0, data.Length);
                stream.Flush();
            });
        }
        catch {
            if (_statusItem.Button != null) _statusItem.Button.Title = "⚠️";
            await Task.Delay(1000);
        }
        finally {
            watch.Stop();
            int delay = 500 - (int)watch.ElapsedMilliseconds;
            if (delay > 0) await Task.Delay(delay);
            await CheckConnectionAsync(); // Actualiza el estado después de enviar
        }
    }

    public override void WillTerminate(NSNotification notification)
    {
        _statusTimer?.Stop();
        if (_activityToken != null) NSProcessInfo.ProcessInfo.EndActivity(_activityToken);
    }
}