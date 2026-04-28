using System.Windows.Media;

using MediaColor = System.Windows.Media.Color;

namespace Re_Color.Models
{
	public sealed class PickedColor
	{
		public MediaColor Color { get; }

		public string Hex => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";
		public string Rgb => $"rgb({Color.R}, {Color.G}, {Color.B})";

		public PickedColor(MediaColor color)
		{
			Color = color;
		}
	}
}