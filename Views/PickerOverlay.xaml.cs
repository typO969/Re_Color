using System.Windows;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using System.Windows.Threading;
using Re_Color.Models;
using WinForms = System.Windows.Forms;

namespace Re_Color.Views;

public partial class PickerOverlay : Window
{
	private readonly DispatcherTimer _timer = new();
	private PickedColor? _currentColor;
	private bool _mouseOverOverlay;

	public event EventHandler<PickedColor>? ColorSampled;

	public PickerOverlay()
	{
		InitializeComponent();
		_timer.Interval = TimeSpan.FromMilliseconds(50);
		_timer.Tick += (_, _) => RefreshPreview();
		MouseEnter += (_, _) => _mouseOverOverlay = true;
		MouseLeave += (_, _) => _mouseOverOverlay = false;
	}

	public void ShowForSampling()
	{
		if (!IsVisible) Show();
		Activate();
		RefreshPreview();
		_timer.Start();
	}

	private void RefreshPreview()
	{
		if (_mouseOverOverlay) return;
		var position = WinForms.Cursor.Position;
		var color = Services.ScreenColorService.GetColorAt(position.X, position.Y);
		var picked = new PickedColor(color);
		var source = PresentationSource.FromVisual(this);
		if (source?.CompositionTarget != null)
		{
			var transform = source.CompositionTarget.TransformFromDevice;
			var dipPoint = transform.Transform(new System.Windows.Point(position.X, position.Y));
			Left = dipPoint.X + 40;
			Top = dipPoint.Y + 40;
		}
		UpdateColor(picked);
	}

	public void UpdateColor(PickedColor picked)
	{
		_currentColor = picked;
		HexText.Text = picked.Hex;
		RgbText.Text = picked.Rgb;
		ColorPreviewRegion.Fill = new SolidColorBrush(picked.Color);
		var gray = ToGrayscale(picked.Color);
		GrayPreviewRegion.Fill = new SolidColorBrush(gray);
	}

	protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
	{
		base.OnMouseLeftButtonDown(e);
		if (_currentColor == null) return;
		ColorSampled?.Invoke(this, _currentColor);
		_timer.Stop();
		Hide();
	}

	private static MediaColor ToGrayscale(MediaColor color)
	{
		var luminance = (byte)Math.Round((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B));
		return MediaColor.FromRgb(luminance, luminance, luminance);
	}

	private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Escape)
		{
			_timer.Stop();
			Hide();
		}
	}
}
