namespace Re_Color.Models;

public sealed class ProjectColorSlot
{
	public string Role { get; set; }
	public string? Modifier { get; set; }
	public string? Hex { get; set; }

	public ProjectColorSlot(string role, string? modifier = null, string? hex = null)
	{
		Role = role;
		Modifier = modifier;
		Hex = hex;
	}
}
