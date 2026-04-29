using System.Windows;
using System.Windows.Media;
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
			const double offset = 10;
			var transformFromDevice = source.CompositionTarget.TransformFromDevice;
			var transformToDevice = source.CompositionTarget.TransformToDevice;

			var overlaySizeInDevice = transformToDevice.Transform(new System.Windows.Vector(ActualWidth, ActualHeight));
			var overlayWidth = overlaySizeInDevice.X;
			var overlayHeight = overlaySizeInDevice.Y;

			var workingArea = WinForms.Screen.FromPoint(position).WorkingArea;
			double leftInDevice = position.X + offset;
			double topInDevice = position.Y + offset;

			if (leftInDevice + overlayWidth > workingArea.Right)
			{
				leftInDevice = position.X - offset - overlayWidth;
			}

			if (topInDevice + overlayHeight > workingArea.Bottom)
			{
				topInDevice = position.Y - offset - overlayHeight;
			}

			leftInDevice = Math.Clamp(leftInDevice, workingArea.Left, workingArea.Right - overlayWidth);
			topInDevice = Math.Clamp(topInDevice, workingArea.Top, workingArea.Bottom - overlayHeight);

			var dipPoint = transformFromDevice.Transform(new System.Windows.Point(leftInDevice, topInDevice));
			Left = dipPoint.X;
			Top = dipPoint.Y;
		}
		UpdateColor(picked);
	}

	public void UpdateColor(PickedColor picked)
	{
		_currentColor = picked;
		HexText.Text = picked.Hex;
		RgbText.Text = picked.Rgb;
		Background = new SolidColorBrush(picked.Color);
	}

	protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
	{
		base.OnMouseLeftButtonDown(e);
		if (_currentColor == null) return;
		ColorSampled?.Invoke(this, _currentColor);
		StatusText.Text = $"Sampled {_currentColor.Hex}";
		_timer.Stop();
		Hide();
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
