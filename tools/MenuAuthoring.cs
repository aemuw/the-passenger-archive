using Godot;

namespace PassengerArchive;

public static class MenuAuthoring
{
	static readonly Color Paper = new(0.86f, 0.82f, 0.69f);
	static readonly Color Ink = new(0.12f, 0.15f, 0.13f);
	public static ArchiveMenu Create()
	{
		var menu = new ArchiveMenu { Name = "ArchiveMenu", Layer = 30, ProcessMode = Node.ProcessModeEnum.Always };
		var root = new Control { Name = "Root" }; root.SetAnchorsPreset(Control.LayoutPreset.FullRect); menu.AddChild(root);
		var background = new ColorRect { Name = "Night", Color = new Color(0.025f, 0.04f, 0.036f, 0.98f) };
		background.SetAnchorsPreset(Control.LayoutPreset.FullRect); root.AddChild(background);
		var window=new TextureRect { Name="ArchiveWindow",Texture=GD.Load<Texture2D>("res://assets/textures/menu_window.svg"),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=Control.MouseFilterEnum.Ignore,Modulate=new Color(1,1,1,.45f) };
		window.AnchorLeft=.70f;window.AnchorRight=.97f;window.AnchorTop=.04f;window.AnchorBottom=.96f;root.AddChild(window);
		var margin = new MarginContainer { Name = "Layout" }; margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 72); margin.AddThemeConstantOverride("margin_right", 72);
		margin.AddThemeConstantOverride("margin_top", 40); margin.AddThemeConstantOverride("margin_bottom", 32); root.AddChild(margin);
		// A simple typeset railway document. No generated illustration, glass cards or neon.
		var scroll = new ScrollContainer { Name = "Scroll", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; margin.AddChild(scroll);
		var box = new VBoxContainer { Name = "Controls", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; box.AddThemeConstantOverride("separation", 10); scroll.AddChild(box);
		Label(box, "Issue", "АРХІВ ПАСАЖИРІВ     /     НІЧНИЙ РЕЙС", 16, new Color(0.57f, 0.64f, 0.56f));
		Label(box, "Title", "THE PASSENGER ARCHIVE", 40, Paper);
		Label(box, "Premise", "Деякі історії можна лише пам’ятати.", 20, Paper);
		var ticket = new PanelContainer { Name = "Ticket" };
		var style = new StyleBoxFlat { BgColor = Paper, ContentMarginLeft = 20, ContentMarginRight = 20, ContentMarginTop = 12, ContentMarginBottom = 12 };
		ticket.AddThemeStyleboxOverride("panel", style); box.AddChild(ticket);
		Label(ticket, "TicketPrint", "КВИТОК № 0001     ·     ВІДПРАВЛЕННЯ 23:41\nПАСАЖИР __________________     ПРИЗНАЧЕННЯ ███████", 18, Ink);
		Button(box, "Continue", "Продовжити подорож");
		Button(box, "NewGame", "Нова подорож");
		Button(box, "Resume", "Повернутися у вагон");
		Button(box, "SettingsButton", "Налаштування");
		var settings = new VBoxContainer { Name = "Settings", Visible = false }; settings.AddThemeConstantOverride("separation", 4); box.AddChild(settings);
		Slider(settings, "Volume", "Гучність", 0, 1, 0.8f);
		Slider(settings, "Sensitivity", "Чутливість миші", 0.2f, 3, 1);
		Slider(settings, "Effects", "Шум і спотворення", 0, 1, 0.45f);
		Slider(settings, "Motion", "Рух камери", 0, 1, 0.6f);
		Slider(settings, "Gore", "Інтенсивність крові", 0, 1, 1);
		Slider(settings, "TextScale", "Розмір діалогів", 0.8f, 1.4f, 1);
		Slider(settings, "Scares", "Різкі появи та звуки", 0, 1, 1);
		Button(box, "Quit", "Вийти");
		Label(box, "Warning", "Містить сцени насильства та історії загибелі людей.\nWASD — рух · E — взаємодія · Tab — квиток · Esc — пауза", 16, new Color(0.57f, 0.64f, 0.56f));
		return menu;
	}
	static void Label(Node parent, string name, string text, int size, Color color)
	{
		var label = new Label { Name = name, Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color); parent.AddChild(label);
	}
	static void Button(Node parent, string name, string text)
	{
		var button = new Button { Name = name, Text = text, Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 34) };
		button.AddThemeFontSizeOverride("font_size", 20); button.AddThemeColorOverride("font_color", Paper);
		var normal = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 4, ContentMarginBottom = 4 };
		var hover = (StyleBoxFlat)normal.Duplicate(); hover.BgColor = new Color(0.12f, 0.18f, 0.15f);
		var focus = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = Paper, BorderWidthLeft = 2 };
		button.AddThemeStyleboxOverride("normal", normal); button.AddThemeStyleboxOverride("hover", hover); button.AddThemeStyleboxOverride("pressed", hover);
		button.AddThemeStyleboxOverride("focus", focus); parent.AddChild(button);
	}
	static void Slider(Node parent, string name, string label, float min, float max, float value)
	{
		var row = new HBoxContainer { Name = name }; parent.AddChild(row);
		Label(row, "Label", label, 17, Paper);
		row.GetNode<Label>("Label").CustomMinimumSize = new Vector2(240, 24);
		var slider = new HSlider { Name = "Slider", MinValue = min, MaxValue = max, Step = 0.01, Value = value, CustomMinimumSize = new Vector2(180, 20), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddChild(slider);
	}
}
