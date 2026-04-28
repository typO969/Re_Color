using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Re_Color.Services
{
	public sealed class HotkeyService : IDisposable
	{
		private const int WM_HOTKEY = 0x0312;
		private const int HOTKEY_ID = 9001;

		private readonly Window _window;
		private HwndSource? _source;

		public event EventHandler? HotkeyPressed;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		public HotkeyService(Window window)
		{
			_window = window;
		}

		public void Register()
		{
			var helper = new WindowInteropHelper(_window);
			_source = HwndSource.FromHwnd(helper.Handle);
			_source?.AddHook(WndProc);

			// Ctrl + Alt + C
			RegisterHotKey(helper.Handle, HOTKEY_ID, 0x0002 | 0x0001, 0x43);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
			{
				HotkeyPressed?.Invoke(this, EventArgs.Empty);
				handled = true;
			}

			return IntPtr.Zero;
		}

		public void Dispose()
		{
			var helper = new WindowInteropHelper(_window);
			UnregisterHotKey(helper.Handle, HOTKEY_ID);
			_source?.RemoveHook(WndProc);
		}
	}
}