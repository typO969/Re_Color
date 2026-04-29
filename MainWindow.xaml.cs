using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using MediaColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfClipboard = System.Windows.Clipboard;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfContextMenu = System.Windows.Controls.ContextMenu;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMenuItem = System.Windows.Controls.MenuItem;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using Re_Color.Models;
using WinForms = System.Windows.Forms;

namespace Re_Color;

public partial class MainWindow : Window
{
	private readonly ObservableCollection<ColorProject> _projects = [];
	private readonly ObservableCollection<PickedColor> _recentColors = [];
	private Services.HotkeyService? _hotkeyService;
	private Views.PickerOverlay? _overlay;
	private WinForms.NotifyIcon? _trayIcon;
	private int _activeSlot = -1;
	private PickedColor? _selectedRecentColor;
	private readonly string _statePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Re_Color", "state.json");

	public MainWindow()
	{
		InitializeComponent();
		Loaded += MainWindow_Loaded;
		Closing += MainWindow_Closing;
		Closed += MainWindow_Closed;
		PreviewKeyDown += MainWindow_PreviewKeyDown;
		PreviewMouseDown += MainWindow_PreviewMouseDown;
		SizeChanged += (_, _) => SaveState();
	}

	private void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		_hotkeyService = new Services.HotkeyService(this);
		_hotkeyService.HotkeyPressed += HotkeyService_HotkeyPressed;
		_hotkeyService.Register();
		butSample.Click += (_, _) => OpenSamplingOverlay();
		butSave.Click += (_, _) => SaveState();
		InitializeTrayIcon();
		LoadState();
		ProjectsList.ItemsSource = _projects;
		if (_projects.Count == 0)
		{
			_projects.Add(new ColorProject { Name = "Marketing Site", DateStarted = DateTime.Today, DateDue = DateTime.Today.AddDays(30) });
		}
		ProjectsList.SelectedIndex = Math.Max(0, ProjectsList.SelectedIndex);
		RenderRecents();
	}

	private void HotkeyService_HotkeyPressed(object? sender, EventArgs e) => OpenSamplingOverlay();

	private void InitializeTrayIcon()
	{
		_trayIcon = new WinForms.NotifyIcon
		{
			Icon = System.Drawing.SystemIcons.Application,
			Text = "Re_Color",
			Visible = true
		};

		var menu = new WinForms.ContextMenuStrip();
		menu.Items.Add("Show", null, (_, _) => ShowFromTray());
		menu.Items.Add("Exit", null, (_, _) => ExitApplication());
		_trayIcon.ContextMenuStrip = menu;
		_trayIcon.DoubleClick += (_, _) => ShowFromTray();
	}

	private void ShowFromTray()
	{
		Show();
		WindowState = WindowState.Normal;
		Activate();
	}

	private void ExitApplication()
	{
		if (_overlay?.IsSampling == true)
		{
			_overlay.StopSampling();
		}

		SaveState();
		_hotkeyService?.Dispose();
		_hotkeyService = null;
		_overlay?.Close();
		_overlay = null;
		if (_trayIcon is not null)
		{
			_trayIcon.Visible = false;
			_trayIcon.Dispose();
			_trayIcon = null;
		}

		Application.Current.Shutdown();
	}

	private void OpenSamplingOverlay()
	{
		_overlay ??= new Views.PickerOverlay();
		_overlay.ColorSampled -= Overlay_ColorSampled;
		_overlay.ColorSampled += Overlay_ColorSampled;
		_overlay.ShowForSampling();
	}

	private void Overlay_ColorSampled(object? sender, PickedColor picked)
	{
		AddRecentColor(picked);
		if (_activeSlot >= 0)
		{
			ApplyColorToActiveSlot(picked.Hex);
			_activeSlot = -1;
		}
	}

	private void AddRecentColor(PickedColor picked)
	{
		if (_recentColors.Any(x => x.Hex == picked.Hex))
		{
			var existing = _recentColors.First(x => x.Hex == picked.Hex);
			_recentColors.Remove(existing);
		}
		_recentColors.Insert(0, picked);
		while (_recentColors.Count > 20) _recentColors.RemoveAt(_recentColors.Count - 1);
		RenderRecents();
		SaveState();
	}

	private void RenderRecents()
	{
		RecentColorsBar.Items.Clear();
		foreach (var color in _recentColors)
		{
			var b = new WpfButton { Width = 34, Height = 34, Margin = new Thickness(4), ToolTip = color.Hex, Background = new SolidColorBrush(color.Color), BorderThickness = new Thickness(1), BorderBrush = WpfBrushes.Black, Tag = color };
			b.Click += (_, _) => { _selectedRecentColor = color; WpfClipboard.SetText(color.Hex); };
			b.PreviewMouseMove += RecentColor_MouseMove;
			RecentColorsBar.Items.Add(b);
		}
	}

  private void RecentColor_MouseMove(object sender, WpfMouseEventArgs e)
	{
		if (e.LeftButton != MouseButtonState.Pressed || sender is not WpfButton b || b.Tag is not PickedColor color) return;
    DragDrop.DoDragDrop(b, color.Hex, WpfDragDropEffects.Copy);
	}

	private void ApplyColorToActiveSlot(string hex)
	{
		if (ProjectsList.SelectedItem is not ColorProject project || _activeSlot < 0 || _activeSlot >= project.Slots.Count) return;
		project.Slots[_activeSlot].Hex = hex;
		RenderSlots(project);
		SaveState();
	}

	private void RenderSlots(ColorProject project)
	{
		SlotsGrid.Children.Clear();
		for (int i = 0; i < project.Slots.Count; i++)
		{
			var slot = project.Slots[i];
			var colorHex = slot.Hex ?? (i % 2 == 0 ? "#FFFFFF" : "#000000");
			var col = (MediaColor)WpfColorConverter.ConvertFromString(colorHex);
			var gray = ToGray(col);
			var top = SwatchBlock(colorHex, col, i, true);
			var bottom = SwatchBlock(ToHex(gray), gray, i, false);
			var panel = new StackPanel();
			panel.Children.Add(top);
			panel.Children.Add(bottom);
			SlotsGrid.Children.Add(panel);
		}
	}

	private Border SwatchBlock(string hex, MediaColor color, int index, bool editable)
	{
		var textColor = GetReadableText(color);
		var border = new Border
		{
			Height = 110,
			Margin = new Thickness(1),
			Background = new SolidColorBrush(color),
			Child = new TextBlock { Text = hex, Foreground = new SolidColorBrush(textColor), FontWeight = FontWeights.Bold, FontSize = 21, Margin = new Thickness(8, 10, 8, 0) },
			ToolTip = editable ? "Right-click to Add/Remove; drag recent colors here" : "Calculated grayscale",
			Tag = index,
			AllowDrop = editable
		};
		if (editable)
		{
			border.ContextMenu = BuildSlotMenu(index);
			border.Drop += Slot_Drop;
		}
		return border;
	}

 private WpfContextMenu BuildSlotMenu(int index)
	{
    var menu = new WpfContextMenu();
		var add = new WpfMenuItem { Header = "Add" };
		add.Click += (_, _) => AddToSlot(index);
      var remove = new WpfMenuItem { Header = "Remove" };
		remove.Click += (_, _) => RemoveFromSlot(index);
		menu.Items.Add(add);
		menu.Items.Add(remove);
		return menu;
	}

	private void AddToSlot(int index)
	{
		_activeSlot = index;
		if (_selectedRecentColor is not null)
		{
			ApplyColorToActiveSlot(_selectedRecentColor.Hex);
			_activeSlot = -1;
			return;
		}
		OpenSamplingOverlay();
	}

	private void RemoveFromSlot(int index)
	{
		if (ProjectsList.SelectedItem is not ColorProject p) return;
		p.Slots[index].Hex = null;
		RenderSlots(p);
		SaveState();
	}

   private void Slot_Drop(object sender, WpfDragEventArgs e)
	{
    if (sender is not Border b || b.Tag is not int index || !e.Data.GetDataPresent(WpfDataFormats.StringFormat)) return;
		var hex = e.Data.GetData(WpfDataFormats.StringFormat) as string;
		if (string.IsNullOrWhiteSpace(hex)) return;
		_activeSlot = index;
		ApplyColorToActiveSlot(hex);
		_activeSlot = -1;
	}

	private static MediaColor ToGray(MediaColor c)
	{
		byte v = (byte)Math.Clamp((int)Math.Round((c.R * 0.299) + (c.G * 0.587) + (c.B * 0.114)), 0, 255);
		return MediaColor.FromRgb(v, v, v);
	}
	private static string ToHex(MediaColor c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
	private static MediaColor GetReadableText(MediaColor bg)
	{
		double luminance = (0.2126 * bg.R + 0.7152 * bg.G + 0.0722 * bg.B) / 255;
		return luminance > 0.5 ? Colors.Black : Colors.White;
	}

	private void AddProject_Click(object sender, RoutedEventArgs e)
	{
		var p = new ColorProject { Name = $"Project {_projects.Count + 1}" };
		_projects.Insert(0, p);
		ProjectsList.SelectedItem = p;
		SaveState();
	}
	private void ProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ProjectsList.SelectedItem is not ColorProject p) return;
		ProjectNameBox.Text = p.Name;
		StartDatePicker.SelectedDate = p.DateStarted;
		DueDatePicker.SelectedDate = p.DateDue;
		RenderSlots(p);
	}
	private void ProjectMeta_Changed(object sender, RoutedEventArgs e)
	{
		if (ProjectsList.SelectedItem is not ColorProject p) return;
		p.Name = string.IsNullOrWhiteSpace(ProjectNameBox.Text) ? "Untitled" : ProjectNameBox.Text.Trim();
		p.DateStarted = StartDatePicker.SelectedDate ?? DateTime.Today;
		p.DateDue = DueDatePicker.SelectedDate;
		ProjectsList.Items.Refresh();
		SaveState();
	}
	private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (SortCombo.SelectedIndex == 1)
		{
			var sorted = _projects.OrderBy(x => x.Name).ToList();
			_projects.Clear(); foreach (var p in sorted) _projects.Add(p);
		}
		else
		{
			var sorted = _projects.OrderByDescending(x => x.DateStarted).ToList();
			_projects.Clear(); foreach (var p in sorted) _projects.Add(p);
		}
		SaveState();
	}

   private void MainWindow_PreviewKeyDown(object sender, WpfKeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			_selectedRecentColor = null;
			_activeSlot = -1;
		}
	}
  private void MainWindow_PreviewMouseDown(object sender, WpfMouseButtonEventArgs e)
	{
     if (e.OriginalSource is not DependencyObject d || FindAncestor<WpfButton>(d) is null)
		{
			_selectedRecentColor = null;
		}
	}
	private static T? FindAncestor<T>(DependencyObject d) where T : DependencyObject
	{
		while (d != null)
		{
			if (d is T t) return t;

			if (d is Visual or Visual3D)
			{
				d = VisualTreeHelper.GetParent(d);
			}
			else
			{
				d = LogicalTreeHelper.GetParent(d);
			}
		}
		return null;
	}

	private void SaveState()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
		var state = new AppState
		{
			Projects = _projects.ToList(),
			RecentHex = _recentColors.Select(x => x.Hex).ToList(),
			WindowWidth = Width,
			WindowHeight = Height
		};
		File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
	}
	private void LoadState()
	{
		if (!File.Exists(_statePath)) return;
		var state = JsonSerializer.Deserialize<AppState>(File.ReadAllText(_statePath));
		if (state is null) return;
		if (state.WindowWidth > 200) Width = state.WindowWidth;
		if (state.WindowHeight > 200) Height = state.WindowHeight;
		_projects.Clear();
		foreach (var p in state.Projects) _projects.Add(p);
		_recentColors.Clear();
		foreach (var hex in state.RecentHex)
		{
			var col = (MediaColor)WpfColorConverter.ConvertFromString(hex);
			_recentColors.Add(new PickedColor(col));
		}
	}
	private void MainWindow_Closed(object? sender, EventArgs e)
	{
		SaveState();
		_hotkeyService?.Dispose();
		if (_trayIcon is not null)
		{
			_trayIcon.Visible = false;
			_trayIcon.Dispose();
			_trayIcon = null;
		}
	}

	private void MainWindow_Closing(object? sender, CancelEventArgs e)
	{
		if (_trayIcon is null) return;

		if (_overlay?.IsSampling == true)
		{
			_overlay.StopSampling();
		}

		e.Cancel = true;
		Hide();
	}

	private sealed class AppState
	{
		public List<ColorProject> Projects { get; set; } = [];
		public List<string> RecentHex { get; set; } = [];
		public double WindowWidth { get; set; }
		public double WindowHeight { get; set; }
	}
}
