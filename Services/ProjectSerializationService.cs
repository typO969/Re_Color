using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Re_Color.Models;

namespace Re_Color.Services;

public sealed class ProjectSerializationService
{
	private const int CurrentSchemaVersion = 1;
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public string SerializeAppState(IReadOnlyCollection<ColorProject> projects, IReadOnlyCollection<string> recentHex, double windowWidth, double windowHeight, double leftColumnWidth = 330)
	{
		var state = new AppStateDto
		{
			SchemaVersion = CurrentSchemaVersion,
			Projects = projects.Select(ToProjectDto).ToList(),
			RecentHex = recentHex.ToList(),
			WindowWidth = windowWidth,
			WindowHeight = windowHeight,
			LeftColumnWidth = leftColumnWidth
		};
		return JsonSerializer.Serialize(state, JsonOptions);
	}

	public (List<ColorProject> Projects, List<string> RecentHex, double WindowWidth, double WindowHeight, double LeftColumnWidth) DeserializeAppState(string json)
	{
		var state = JsonSerializer.Deserialize<AppStateDto>(json, JsonOptions) ?? new AppStateDto();
		return (state.Projects.Select(ToProject).ToList(), state.RecentHex, state.WindowWidth, state.WindowHeight, state.LeftColumnWidth);
	}

	public string SerializeProjects(IEnumerable<ColorProject> projects)
	{
		var payload = new ProjectExportDto { SchemaVersion = CurrentSchemaVersion, Projects = projects.Select(ToProjectDto).ToList() };
		return JsonSerializer.Serialize(payload, JsonOptions);
	}

	public List<ColorProject> DeserializeProjects(string json)
	{
		var payload = JsonSerializer.Deserialize<ProjectExportDto>(json, JsonOptions) ?? new ProjectExportDto();
		return payload.Projects.Select(ToProject).ToList();
	}

	public byte[] CreateAseSwatch(IEnumerable<ColorProject> projects)
	{
		var entries = projects.SelectMany(p => p.Slots.Where(s => !string.IsNullOrWhiteSpace(s.Hex))
			.Select(s => (Name: $"{p.Name} - {s.Role}", Hex: s.Hex!))).ToList();

		using var ms = new MemoryStream();
		using var bw = new BinaryWriter(ms, Encoding.BigEndianUnicode);

		bw.Write(new byte[] { 0x41, 0x53, 0x45, 0x46, 0x00, 0x01, 0x00, 0x00 });
		bw.Write(ToBigEndian((uint)entries.Count));
		foreach (var entry in entries)
		{
			bw.Write(ToBigEndian((ushort)0x0001));
			var blockData = BuildAseColorBlock(entry.Name, entry.Hex);
			bw.Write(ToBigEndian((uint)blockData.Length));
			bw.Write(blockData);
		}
		bw.Flush();
		return ms.ToArray();
	}

	private static byte[] BuildAseColorBlock(string name, string hex)
	{
		var (r, g, b) = ParseHex(hex);
		using var ms = new MemoryStream();
		using var bw = new BinaryWriter(ms);
		var encodedName = Encoding.BigEndianUnicode.GetBytes(name);
		var charCount = (ushort)(name.Length + 1);
		bw.Write(ToBigEndian(charCount));
		bw.Write(encodedName);
		bw.Write((byte)0x00);
		bw.Write((byte)0x00);
		bw.Write(new byte[] { 0x52, 0x47, 0x42, 0x20 });
		bw.Write(ToBigEndian(FloatToBytes(r / 255f)));
		bw.Write(ToBigEndian(FloatToBytes(g / 255f)));
		bw.Write(ToBigEndian(FloatToBytes(b / 255f)));
		bw.Write(ToBigEndian((ushort)0x0000));
		bw.Flush();
		return ms.ToArray();
	}

	private static ColorProjectDto ToProjectDto(ColorProject p) => new()
	{
		Name = p.Name,
		DateStarted = p.DateStarted,
		DateDue = p.DateDue,
		Slots = p.Slots.Select(s => new ProjectColorSlotDto { Role = s.Role, Modifier = s.Modifier, Hex = s.Hex }).ToList()
	};

	private static ColorProject ToProject(ColorProjectDto dto)
	{
		var p = new ColorProject
		{
			Name = string.IsNullOrWhiteSpace(dto.Name) ? "Untitled" : dto.Name,
			DateStarted = dto.DateStarted,
			DateDue = dto.DateDue
		};
		p.Slots.Clear();
		foreach (var slot in dto.Slots.DefaultIfEmpty(new ProjectColorSlotDto { Role = "Primary" }))
		{
			p.Slots.Add(new ProjectColorSlot(slot.Role ?? "Primary", slot.Modifier, slot.Hex));
		}
		return p;
	}

	private static (byte R, byte G, byte B) ParseHex(string hex)
	{
		var clean = hex.TrimStart('#');
		if (clean.Length != 6) throw new FormatException($"Invalid hex color: {hex}");
		return (
			byte.Parse(clean[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			byte.Parse(clean[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			byte.Parse(clean[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
	}

	private static byte[] ToBigEndian(ushort value) => [ (byte)(value >> 8), (byte)(value & 0xFF) ];
	private static byte[] ToBigEndian(uint value) => [ (byte)(value >> 24), (byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF) ];
	private static byte[] ToBigEndian(byte[] bytes) => BitConverter.IsLittleEndian ? bytes.Reverse().ToArray() : bytes;
	private static byte[] FloatToBytes(float value) => BitConverter.GetBytes(value);

	private sealed class AppStateDto
	{
		public int SchemaVersion { get; set; } = CurrentSchemaVersion;
		public List<ColorProjectDto> Projects { get; set; } = [];
		public List<string> RecentHex { get; set; } = [];
		public double WindowWidth { get; set; }
		public double WindowHeight { get; set; }
		public double LeftColumnWidth { get; set; } = 330;
	}

	private sealed class ProjectExportDto
	{
		public int SchemaVersion { get; set; } = CurrentSchemaVersion;
		public List<ColorProjectDto> Projects { get; set; } = [];
	}

	private sealed class ColorProjectDto
	{
		public string Name { get; set; } = "Untitled";
		public DateTime DateStarted { get; set; } = DateTime.Today;
		public DateTime? DateDue { get; set; }
		public List<ProjectColorSlotDto> Slots { get; set; } = [];
	}

	private sealed class ProjectColorSlotDto
	{
		public string? Role { get; set; }
		public string? Modifier { get; set; }
		public string? Hex { get; set; }
	}
}
