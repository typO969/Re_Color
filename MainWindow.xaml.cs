using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using MediaColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfClipboard = System.Windows.Clipboard;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfOrientation = System.Windows.Controls.Orientation;
using Re_Color.Models;
using WinForms = System.Windows.Forms;

namespace Re_Color;

public partial class MainWindow : Window
{
	private readonly ObservableCollection<ColorProject> _projects = [];
	private readonly ObservableCollection<PickedColor> _recentColors = [];
	private Services.HotkeyService? _hotkeyService;
	private Views.PickerOverlay? _overlay;
	private int _activeSlot = 0;

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

		_projects.Add(new ColorProject { Name = "Marketing Site", DateStarted = DateTime.Today, DateDue = DateTime.Today.AddDays(30) });
		ProjectsList.ItemsSource = _projects;
		ProjectsList.SelectedIndex = 0;
		RenderRecents();
	}

	private void HotkeyService_HotkeyPressed(object? sender, EventArgs e)
	{
		var position = WinForms.Cursor.Position;
		var color = Services.ScreenColorService.GetColorAt(position.X, position.Y);
		var picked = new PickedColor(color);

		_overlay ??= new Views.PickerOverlay();
		_overlay.Show();
		_overlay.Activate();
		_overlay.Left = position.X + 20;
		_overlay.Top = position.Y + 20;
		_overlay.UpdateColor(picked);

		AddRecentColor(picked);
		ApplyColorToActiveSlot(picked.Hex);
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
	}

	private void RenderRecents()
	{
		RecentColorsBar.Items.Clear();
      foreach (var color in _recentColors)
		{
			var b = new WpfButton { Width = 34, Height = 34, Margin = new Thickness(4), ToolTip = color.Hex, Background = new SolidColorBrush(color.Color), BorderThickness = new Thickness(1), BorderBrush = WpfBrushes.Black };
       b.Click += (_, _) => WpfClipboard.SetText(color.Hex);
			RecentColorsBar.Items.Add(b);
		}
	}

	private void ApplyColorToActiveSlot(string hex)
	{
		if (ProjectsList.SelectedItem is not ColorProject project || _activeSlot < 0 || _activeSlot >= project.Slots.Count) return;
		project.Slots[_activeSlot].Hex = hex;
		RenderSlots(project);
	}

	private void RenderSlots(ColorProject project)
	{
		SlotsGrid.Children.Clear();
		for (int i = 0; i < project.Slots.Count; i++)
		{
			var slot = project.Slots[i];
			var colorHex = slot.Hex ?? "#2D2D2D";
       var col = (MediaColor)WpfColorConverter.ConvertFromString(colorHex);
			var gray = ToGray(col);
			var top = SwatchBlock(colorHex, col, i, true);
			var bottom = SwatchBlock(ToHex(gray), gray, i, false);
			var panel = new StackPanel();
			panel.Children.Add(top);
			panel.Children.Add(bottom);
         var rowBtns = new StackPanel { Orientation = WpfOrientation.Horizontal, HorizontalAlignment = WpfHorizontalAlignment.Center };
			var add = new WpfButton { Content = "Add", Margin = new Thickness(2), Tag = i };
			var remove = new WpfButton { Content = "Remove", Margin = new Thickness(2), Tag = i };
			add.Click += SlotAdd_Click;
			remove.Click += SlotRemove_Click;
			rowBtns.Children.Add(add);
			rowBtns.Children.Add(remove);
			panel.Children.Add(rowBtns);
			SlotsGrid.Children.Add(panel);
		}
	}

 private Border SwatchBlock(string hex, MediaColor color, int index, bool editable)
	{
		var textColor = GetReadableText(color);
		return new Border
		{
			Height = 110,
			Margin = new Thickness(1),
			Background = new SolidColorBrush(color),
			Child = new TextBlock
			{
				Text = hex,
				Foreground = new SolidColorBrush(textColor),
				FontWeight = FontWeights.Bold,
				FontSize = 21,
				Margin = new Thickness(8, 10, 8, 0)
			},
			ToolTip = editable ? "Click Add after sampling a color" : "Calculated grayscale",
			Tag = index
		};
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
	}

	private void ProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ProjectsList.SelectedItem is not ColorProject p) return;
		ProjectNameBox.Text = p.Name;
		StartDatePicker.SelectedDate = p.DateStarted;
		DueDatePicker.SelectedDate = p.DateDue;
		RenderSlots(p);
	}

	private void ProjectMeta_Changed(object sender, EventArgs e)
	{
		if (ProjectsList.SelectedItem is not ColorProject p) return;
		p.Name = string.IsNullOrWhiteSpace(ProjectNameBox.Text) ? "Untitled" : ProjectNameBox.Text.Trim();
		p.DateStarted = StartDatePicker.SelectedDate ?? DateTime.Today;
		p.DateDue = DueDatePicker.SelectedDate;
		ProjectsList.Items.Refresh();
	}

	private void SlotAdd_Click(object sender, RoutedEventArgs e)
	{
    if (sender is WpfButton b && b.Tag is int idx) _activeSlot = idx;
	}

	private void SlotRemove_Click(object sender, RoutedEventArgs e)
	{
    if (ProjectsList.SelectedItem is not ColorProject p || sender is not WpfButton b || b.Tag is not int idx) return;
		p.Slots[idx].Hex = null;
		RenderSlots(p);
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
	}

	private void MainWindow_Closed(object? sender, EventArgs e) => _hotkeyService?.Dispose();
}
