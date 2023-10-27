using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using RazorCX.FindMissingDrawings.Views;
using Prism.Ioc;
using System.Windows;

namespace RazorCX.FindMissingDrawings
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        protected override Window CreateShell()
        {
	        if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
	        {
		        SetForegroundWindow(GetCurrentInstanceWindowHandle());
		        Process.GetCurrentProcess().Kill();
	        }

	        return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {

        }

        private static IntPtr GetCurrentInstanceWindowHandle()
        {
	        IntPtr num = IntPtr.Zero;

	        var currentProcess = Process.GetCurrentProcess();
	        var existingProcess = Process.GetProcessesByName(currentProcess.ProcessName)
		        .FirstOrDefault(p => p.Id != currentProcess.Id);

	        if (existingProcess != null)
		        num = existingProcess.MainWindowHandle;

	        return num;
        }
	}
}
