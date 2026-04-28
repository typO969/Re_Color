using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WinForms = System.Windows.Forms;

namespace Re_Color
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		private Services.HotkeyService? _hotkeyService;
		private Views.PickerOverlay? _overlay;

		public MainWindow()
		{
			InitializeComponent();

			Loaded += MainWindow_Loaded;
			Closed += MainWindow_Closed;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			_hotkeyService = new Services.HotkeyService(this);
			_hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
			_hotkeyService.Register();

			Hide();
		}

		private void HotkeyService_HotkeyPressed(object? sender, EventArgs e)
		{
			var position = WinForms.Cursor.Position;
			var color = Services.ScreenColorService.GetColorAt(position.X, position.Y);
			var picked = new Models.PickedColor(color);

			if (_overlay == null)
			{
				_overlay = new Views.PickerOverlay();
				_overlay.Show();
			}

			_overlay.Left = position.X + 20;
			_overlay.Top = position.Y + 20;

			_overlay.UpdateColor(picked);
		}

		private void MainWindow_Closed(object? sender, EventArgs e)
		{
			_hotkeyService?.Dispose();
		}
	}
}