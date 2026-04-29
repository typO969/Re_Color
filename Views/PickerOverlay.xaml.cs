using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Re_Color.Models;
using WinForms = System.Windows.Forms;

namespace Re_Color.Views;

public partial class PickerOverlay : Window
{
	private readonly DispatcherTimer _timer = new();
	private Services.GlobalMouseHookService? _mouseHook;
	private PickedColor? _currentColor;
	private bool _mouseOverOverlay;
	private bool _samplingActive;

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
		_samplingActive = true;
		EnsureMouseHook();
		if (!IsVisible) Show();
		Activate();
		RefreshPreview();
		_timer.Start();
	}

	private void EnsureMouseHook()
	{
		_mouseHook ??= new Services.GlobalMouseHookService();
		_mouseHook.LeftButtonDown -= OnGlobalLeftButtonDown;
		_mouseHook.LeftButtonDown += OnGlobalLeftButtonDown;
		_mouseHook.Start();
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
		Background = new SolidColorBrush(picked.Color);
	}

	protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
	{
		base.OnMouseLeftButtonDown(e);
		if (!_samplingActive || !_mouseOverOverlay) return;
		if (_currentColor == null) return;
		CompleteSampling(_currentColor);
	}

	private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == System.Windows.Input.Key.Escape)
		{
			EndSampling();
		}
	}

	private void OnGlobalLeftButtonDown(object? sender, Services.GlobalMouseLeftButtonDownEventArgs e)
	{
		if (!_samplingActive) return;
		e.Handled = true;

		Dispatcher.Invoke(() =>
		{
			if (!_samplingActive) return;
			var color = Services.ScreenColorService.GetColorAt(e.ScreenX, e.ScreenY);
			var picked = new PickedColor(color);
			UpdateColor(picked);
			CompleteSampling(picked);
		});
	}

	private void CompleteSampling(PickedColor picked)
	{
		ColorSampled?.Invoke(this, picked);
		StatusText.Text = $"Sampled {picked.Hex}";
		EndSampling();
	}

	private void EndSampling()
	{
		_samplingActive = false;
		_timer.Stop();
		_mouseHook?.Stop();
		Hide();
	}

	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);
		_mouseHook?.Dispose();
		_mouseHook = null;
	}
}
