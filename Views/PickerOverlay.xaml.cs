using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

using WinForms = System.Windows.Forms;

namespace Re_Color.Views
{
    /// <summary>
    /// Interaction logic for PickerOverlay.xaml
    /// </summary>
    public partial class PickerOverlay : Window
    {

		private readonly DispatcherTimer _timer = new();
		private Models.PickedColor? _currentColor;
		private bool _mouseOverOverlay;

		public PickerOverlay()
        {
            InitializeComponent();

			_timer.Interval = TimeSpan.FromMilliseconds(50);

			_timer.Tick += (_, _) =>
			{
				if (_mouseOverOverlay)
					return;

				var position = WinForms.Cursor.Position;
				var color = Services.ScreenColorService.GetColorAt(position.X, position.Y);
				var picked = new Models.PickedColor(color);

				var source = PresentationSource.FromVisual(this);

				if (source?.CompositionTarget != null)
				{
					var transform = source.CompositionTarget.TransformFromDevice;
					var dipPoint = transform.Transform(new System.Windows.Point(position.X, position.Y));

					Left = dipPoint.X + 40;
					Top = dipPoint.Y + 40;
				}

				UpdateColor(picked);
			};

			_timer.Start();

			MouseEnter += (_, _) => _mouseOverOverlay = true;
			MouseLeave += (_, _) => _mouseOverOverlay = false;
		}

		public void UpdateColor(Models.PickedColor picked)
		{
			_currentColor = picked;

			HexText.Text = picked.Hex;
			RgbText.Text = picked.Rgb;

			Background = new System.Windows.Media.SolidColorBrush(picked.Color);
		}

		protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);

			if (_currentColor == null)
				return;

			System.Windows.Clipboard.SetText(_currentColor.Hex);

			StatusText.Text = $"Copied {_currentColor.Hex}";
			Hide();
		}

		private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Escape)
			{
				Hide();
			}
		}
	}
}
