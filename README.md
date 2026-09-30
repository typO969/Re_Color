# Re_Color

## Genesis

My many many requests sent to https://github.com/microsoft/PowerToys were ignored and denied regarding an upgrade to their color picker.

Specifically, allowing a user to organize the colors by projects, and those by date and other meaningful metadata.

So I built it myself. I also added a sweet feature that auto-magically displays all the gray values of color sets to make sure you're not burying yourself in contrast!

I'm sure three are many bugs left to find and fix, but it is what it is.

## Import/Export

- **App JSON Import/Export**: Use the **Import** and **Export** buttons in the project pane to load/save full Re_Color project data, including metadata (name, dates) and slot colors.
- JSON files are schema-versioned (`schemaVersion`) for forward compatibility.
- **Swatch Export**: Use **Export ASE** to write an Adobe Swatch Exchange (`.ase`) palette from all populated project slots.

### Supported targets

- Adobe apps that support ASE palettes (e.g., Photoshop, Illustrator).
- Affinity apps with ASE palette import support.

## GUI & Appearance

### The Color Picker:
  <img width="752" alt="theColorPicker" src="https://github.com/user-attachments/assets/4af065f6-370b-4b9c-b164-a91f5ac0f40d" />

### The Manager Window:
  <img width="900" alt="theManagerWindow" src="https://github.com/user-attachments/assets/ed45e557-e12d-4186-ad8b-c517eb115055" />
