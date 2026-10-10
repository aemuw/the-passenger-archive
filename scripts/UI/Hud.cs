using System;
using Godot;

namespace PassengerArchive;

/// <summary>All 2D UI, built in code: prompt, dialogue + choices, ticket, fades, blood, death card. Intentionally minimal.</summary>
public partial class Hud : CanvasLayer
{
	static readonly Color Amber = new(0.95f, 0.78f, 0.35f);
	static readonly Color Paper = new(0.9f, 0.86f, 0.74f);
	static readonly Color HeroBlue = new(0.62f, 0.78f, 1.0f);

	Label _prompt, _notify, _caption, _dlgName, _dlgText, _dlgHint, _bookText, _death;
	PanelContainer _dlgPanel, _book, _choicePanel;
	VBoxContainer _choiceBox;
	Control _gore;
	ColorRect _fade, _flash;
	Tween _typeTween, _notifyTween, _captionTween;
	double _dlgTimer;
	bool _sticky;
	string[] _choiceTexts = Array.Empty<string>();
	readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();
		Layer = 10;
		var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		// Crosshair
		var dot = new ColorRect { Color = new Color(1, 1, 1, 0.55f), MouseFilter = Control.MouseFilterEnum.Ignore };
		dot.AnchorLeft = dot.AnchorRight = dot.AnchorTop = dot.AnchorBottom = 0.5f;
		dot.OffsetLeft = -2; dot.OffsetRight = 2; dot.OffsetTop = -2; dot.OffsetBottom = 2;
		root.AddChild(dot);

		_prompt = MakeLabel(root, 0.2f, 0.60f, 0.8f, 0.72f, 20, Paper, HorizontalAlignment.Center);
		_notify = MakeLabel(root, 0.2f, 0.06f, 0.8f, 0.14f, 20, Amber, HorizontalAlignment.Center);
		_caption = MakeLabel(root, 0.1f, 0.38f, 0.9f, 0.55f, 38, Amber, HorizontalAlignment.Center);
		_caption.Modulate = new Color(1, 1, 1, 0);

		// Choices sit right above the dialogue box
		_choicePanel = MakePanel(root, 0.15f, 0.72f, 0.85f, 0.72f, new Color(0, 0, 0, 0.66f));
		_choicePanel.GrowVertical = Control.GrowDirection.Begin;   // anchored at the dialogue box, grows upwards with the number of options
		_choiceBox = new VBoxContainer();
		_choiceBox.AddThemeConstantOverride("separation", 6);
		_choicePanel.AddChild(_choiceBox);
		_choicePanel.Visible = false;

		_dlgPanel = MakePanel(root, 0.15f, 0.74f, 0.85f, 0.95f, new Color(0, 0, 0, 0.78f));
		var box = new VBoxContainer();
		_dlgPanel.AddChild(box);
		_dlgName = new Label();
		_dlgName.AddThemeColorOverride("font_color", Amber);
		_dlgName.AddThemeFontSizeOverride("font_size", 20);
		_dlgText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
		_dlgText.AddThemeColorOverride("font_color", Paper);
		_dlgText.AddThemeFontSizeOverride("font_size", 22);
		_dlgHint = new Label { Text = "▼", HorizontalAlignment = HorizontalAlignment.Right };
		_dlgHint.AddThemeColorOverride("font_color", new Color(Amber, 0.8f));
		_dlgHint.AddThemeFontSizeOverride("font_size", 16);
		box.AddChild(_dlgName);
		box.AddChild(_dlgText);
		box.AddChild(_dlgHint);
		_dlgPanel.Visible = false;

		_book = MakePanel(root, 0.2f, 0.08f, 0.8f, 0.92f, new Color(0.08f, 0.06f, 0.04f, 0.94f));
		_bookText = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
		_bookText.AddThemeColorOverride("font_color", Paper);
		_bookText.AddThemeFontSizeOverride("font_size", 20);
		_book.AddChild(_bookText);
		_book.Visible = false;

		// Blood lives under the fade so the fade can swallow it.
		_gore = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		_gore.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(_gore);

		_flash = new ColorRect { Color = new Color(0.55f, 0.02f, 0.02f, 0f), MouseFilter = Control.MouseFilterEnum.Ignore };
		_flash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(_flash);

		_fade = new ColorRect { Color = new Color(0, 0, 0, 1), MouseFilter = Control.MouseFilterEnum.Ignore };
		_fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.AddChild(_fade);

		_death = MakeLabel(root, 0.1f, 0.2f, 0.9f, 0.8f, 28, new Color(0.8f, 0.15f, 0.12f), HorizontalAlignment.Center);
		_death.VerticalAlignment = VerticalAlignment.Center;
		_death.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (_dlgPanel.Visible)
		{
			if (!_sticky)
			{
				_dlgTimer -= delta;
				if (_dlgTimer <= 0) _dlgPanel.Visible = false;
			}
			_dlgHint.Visible = _sticky && !IsTyping && !_choicePanel.Visible;
		}
	}

	public void SetPrompt(string text) => _prompt.Text = text ?? "";

	// ───────────── dialogue ─────────────
	/// <summary>sticky = stays until hidden (conversations). Otherwise it fades out after a while (overheard lines).</summary>
	public void ShowDialogue(string speaker, string text, bool sticky = false)
	{
		_sticky = sticky;
		_dlgName.Text = speaker;
		_dlgName.AddThemeColorOverride("font_color", speaker == "Ти" ? HeroBlue : Amber);
		_dlgText.Text = text;
		_dlgText.VisibleRatio = 0f;
		_dlgPanel.Visible = true;
		_dlgHint.Visible = false;
		_dlgTimer = 4.0 + text.Length * 0.05;
		_typeTween?.Kill();
		_typeTween = CreateTween();
		_typeTween.TweenProperty(_dlgText, "visible_ratio", 1.0f, Mathf.Max(0.3, text.Length * 0.022));
	}

	public bool IsTyping => _dlgPanel.Visible && _dlgText.VisibleRatio < 0.999f;

	public void FinishTyping()
	{
		_typeTween?.Kill();
		_dlgText.VisibleRatio = 1f;
	}

	public void HideDialogue()
	{
		_dlgPanel.Visible = false;
		_sticky = false;
		HideChoices();
	}

	public void ShowChoices(string[] texts, int selected)
	{
		_choiceTexts = texts;
		ClearChoiceNodes();
		for (int i = 0; i < texts.Length; i++)
		{
			var lbl = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, Name = "c" + i };
			lbl.AddThemeFontSizeOverride("font_size", 21);
			_choiceBox.AddChild(lbl);
		}
		SetChoice(selected);
		_choicePanel.Visible = true;
	}

	public void SetChoice(int selected)
	{
		for (int i = 0; i < _choiceTexts.Length && i < _choiceBox.GetChildCount(); i++)
		{
			var lbl = (Label)_choiceBox.GetChild(i);
			bool on = i == selected;
			lbl.Text = (on ? "▸ " : "   ") + (i + 1) + ". " + _choiceTexts[i];
			lbl.AddThemeColorOverride("font_color", on ? Amber : new Color(Paper, 0.72f));
		}
	}

	public void HideChoices()
	{
		_choicePanel.Visible = false;
		_choiceTexts = Array.Empty<string>();
		ClearChoiceNodes();
	}

	void ClearChoiceNodes()
	{
		foreach (var c in _choiceBox.GetChildren())
		{
			_choiceBox.RemoveChild(c);   // removed at once, so indices of the new labels start at 0
			c.QueueFree();
		}
	}

	// ───────────── captions & notes ─────────────
	public void Notify(string text, float seconds = 3.5f)
	{
		_notify.Text = text;
		_notify.Modulate = Colors.White;
		_notifyTween?.Kill();
		_notifyTween = CreateTween();
		_notifyTween.TweenInterval(seconds);
		_notifyTween.TweenProperty(_notify, "modulate:a", 0f, 0.8f);
	}

	public void Caption(string text, float hold = 3f)
	{
		_caption.Text = text;
		_captionTween?.Kill();
		_captionTween = CreateTween();
		_captionTween.TweenProperty(_caption, "modulate:a", 1f, 0.8f);
		_captionTween.TweenInterval(hold);
		_captionTween.TweenProperty(_caption, "modulate:a", 0f, 1.2f);
	}

	public void ToggleBook(string text)
	{
		_book.Visible = !_book.Visible;
		_bookText.Text = text;
	}

	public bool BookOpen => _book.Visible;
	public void CloseBook() => _book.Visible = false;

	// ───────────── fade, blood, cards ─────────────
	public Tween Fade(float alpha, float seconds)
	{
		var tw = CreateTween();
		tw.TweenProperty(_fade, "color:a", alpha, seconds);
		return tw;
	}

	/// <summary>A red flash that decays — the moment of impact.</summary>
	public void BloodFlash(float strength = 0.8f)
	{
		_flash.Color = new Color(0.55f, 0.02f, 0.02f, strength);
		var tw = CreateTween();
		tw.TweenProperty(_flash, "color:a", 0f, 0.9f);
	}

	/// <summary>Blocky blood splashes on the "lens" (matches the PS1 look); some of them run down.</summary>
	public void Splatter(int count)
	{
		var size = GetViewport().GetVisibleRect().Size;
		for (int i = 0; i < count; i++)
		{
			float w = _rng.RandfRange(14f, 90f);
			float h = _rng.RandfRange(14f, 70f);
			var r = new ColorRect
			{
				Color = new Color(_rng.RandfRange(0.35f, 0.62f), 0.01f, 0.02f, _rng.RandfRange(0.55f, 0.9f)),
				MouseFilter = Control.MouseFilterEnum.Ignore,
				Position = new Vector2(_rng.RandfRange(0f, size.X - w), _rng.RandfRange(0f, size.Y * 0.8f)),
				Size = new Vector2(w, h),
			};
			_gore.AddChild(r);
			if (_rng.Randf() < 0.45f)
			{
				var tw = CreateTween();
				tw.TweenProperty(r, "size:y", h + _rng.RandfRange(60f, 260f), _rng.RandfRange(2.5f, 6f));
			}
		}
	}

	public void ClearGore()
	{
		foreach (var c in _gore.GetChildren()) c.QueueFree();
		_flash.Color = new Color(0.55f, 0.02f, 0.02f, 0f);
	}

	public void ShowDeath(string text, bool ending = false)
	{
		_death.AddThemeColorOverride("font_color", ending ? Amber : new Color(0.8f, 0.15f, 0.12f));
		_death.Text = text;
		_death.Visible = true;
		_fade.Color = new Color(0, 0, 0, 1);
		_dlgPanel.Visible = false;
		_book.Visible = false;
		HideChoices();
	}

	public void HideDeath() => _death.Visible = false;

	public void SetFadeInstant(float a) => _fade.Color = new Color(0, 0, 0, a);

	// ───────── helpers ─────────
	static Label MakeLabel(Control parent, float l, float t, float r, float b, int size, Color color, HorizontalAlignment align)
	{
		var lbl = new Label
		{
			HorizontalAlignment = align,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		lbl.AddThemeColorOverride("font_color", color);
		lbl.AddThemeColorOverride("font_outline_color", Colors.Black);
		lbl.AddThemeConstantOverride("outline_size", 6);
		lbl.AddThemeFontSizeOverride("font_size", size);
		Anchor(lbl, l, t, r, b);
		parent.AddChild(lbl);
		return lbl;
	}

	static PanelContainer MakePanel(Control parent, float l, float t, float r, float b, Color bg)
	{
		var p = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		var style = new StyleBoxFlat
		{
			BgColor = bg,
			BorderColor = new Color(0.95f, 0.78f, 0.35f, 0.6f),
			ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 12, ContentMarginBottom = 12,
		};
		style.SetBorderWidthAll(2);
		p.AddThemeStyleboxOverride("panel", style);
		Anchor(p, l, t, r, b);
		parent.AddChild(p);
		return p;
	}

	static void Anchor(Control c, float l, float t, float r, float b)
	{
		c.AnchorLeft = l; c.AnchorTop = t; c.AnchorRight = r; c.AnchorBottom = b;
		c.OffsetLeft = c.OffsetTop = c.OffsetRight = c.OffsetBottom = 0;
	}
}
