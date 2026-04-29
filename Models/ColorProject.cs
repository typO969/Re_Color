namespace Re_Color.Models;

public sealed class ColorProject
{
	public string Name { get; set; } = "New Project";
	public DateTime DateStarted { get; set; } = DateTime.Today;
	public DateTime? DateDue { get; set; }
	public List<ProjectColorSlot> Slots { get; } =
	[
		new("Accent 1", "Light", null),
		new("Secondary", "Links", null),
		new("Primary", "Brand", null),
		new("Background", "Optional", null),
		new("Accent 2", "Dark", null),
	];

	public string DisplayLine => $"{Name} • {DateStarted:yyyy-MM-dd}";
}
