using System;
using System.Runtime.InteropServices;

using MediaColor = System.Windows.Media.Color;

namespace Re_Color.Services
{
	public static class ScreenColorService
	{
		[DllImport("user32.dll")]
		private static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("user32.dll")]
		private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

		[DllImport("gdi32.dll")]
		private static extern uint GetPixel(IntPtr hdc, int x, int y);

		public static MediaColor GetColorAt(int x, int y)
		{
			IntPtr hdc = GetDC(IntPtr.Zero);

			try
			{
				uint pixel = GetPixel(hdc, x, y);

				byte r = (byte) (pixel & 0x000000FF);
				byte g = (byte) ((pixel & 0x0000FF00) >> 8);
				byte b = (byte) ((pixel & 0x00FF0000) >> 16);

				return MediaColor.FromRgb(r, g, b);
			}
			finally
			{
				ReleaseDC(IntPtr.Zero, hdc);
			}
		}
	}
}