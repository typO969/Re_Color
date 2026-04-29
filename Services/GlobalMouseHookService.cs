using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Re_Color.Services;

public sealed class GlobalMouseHookService : IDisposable
{
	private const int WhMouseLl = 14;
	private const int WmLButtonDown = 0x0201;

	private readonly LowLevelMouseProc _proc;
	private IntPtr _hookHandle = IntPtr.Zero;
	private bool _disposed;

	public event EventHandler<GlobalMouseLeftButtonDownEventArgs>? LeftButtonDown;

	public GlobalMouseHookService()
	{
		_proc = HookCallback;
	}

	public void Start()
	{
		ThrowIfDisposed();
		if (_hookHandle != IntPtr.Zero) return;

		using var process = Process.GetCurrentProcess();
		using var module = process.MainModule ?? throw new InvalidOperationException("Failed to resolve current process module.");
		_hookHandle = SetWindowsHookEx(WhMouseLl, _proc, GetModuleHandle(module.ModuleName), 0);
		if (_hookHandle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
	}

	public void Stop()
	{
		if (_hookHandle == IntPtr.Zero) return;
		_ = UnhookWindowsHookEx(_hookHandle);
		_hookHandle = IntPtr.Zero;
	}

	private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0 && wParam == (IntPtr) WmLButtonDown)
		{
			var data = Marshal.PtrToStructure<MsLlHookStruct>(lParam);
			var args = new GlobalMouseLeftButtonDownEventArgs(data.pt.x, data.pt.y);
			LeftButtonDown?.Invoke(this, args);
			if (args.Handled) return (IntPtr) 1;
		}

		return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
	}

	public void Dispose()
	{
		if (_disposed) return;
		Stop();
		_disposed = true;
		GC.SuppressFinalize(this);
	}

	private void ThrowIfDisposed()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

	[StructLayout(LayoutKind.Sequential)]
	private struct Point
	{
		public int x;
		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MsLlHookStruct
	{
		public Point pt;
		public uint mouseData;
		public uint flags;
		public uint time;
		public IntPtr dwExtraInfo;
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll")]
	private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr GetModuleHandle(string? lpModuleName);
}

public sealed class GlobalMouseLeftButtonDownEventArgs(int screenX, int screenY) : EventArgs
{
	public int ScreenX { get; } = screenX;
	public int ScreenY { get; } = screenY;
	public bool Handled { get; set; }
}
