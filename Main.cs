using AppKit;
using IR_SecondaryScreen;

NSApplication.Init();

// Esto le dice a macOS: "Usa la clase AppDelegate para manejar la app"
var appDelegate = new AppDelegate();
NSApplication.SharedApplication.Delegate = appDelegate;

NSApplication.Main(args);