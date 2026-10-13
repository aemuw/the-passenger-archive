using System;
using Godot;

namespace PassengerArchive;

/// <summary>Behaviour only; the complete layout lives in scenes/ui/ArchiveMenu.tscn.</summary>
public partial class ArchiveMenu : CanvasLayer
{
	public Action NewGame, ContinueGame, ResumeGame, QuitGame;
	public Action SettingsChanged;
	bool _inRun;
	Control _root, _settings;
	Button _resume, _continue;
	public bool Open => _root.Visible;
	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_root = GetNode<Control>("Root");
		_settings = GetNode<Control>("Root/Layout/Scroll/Controls/Settings");
		_resume = GetNode<Button>("Root/Layout/Scroll/Controls/Resume");
		_continue = GetNode<Button>("Root/Layout/Scroll/Controls/Continue");
		GetNode<Button>("Root/Layout/Scroll/Controls/NewGame").Pressed += () => NewGame?.Invoke();
		_continue.Pressed += () => ContinueGame?.Invoke();
		_resume.Pressed += () => ResumeGame?.Invoke();
		GetNode<Button>("Root/Layout/Scroll/Controls/SettingsButton").Pressed += () => _settings.Visible = !_settings.Visible;
		GetNode<Button>("Root/Layout/Scroll/Controls/Quit").Pressed += () => QuitGame?.Invoke();
		ConnectSlider("Volume", UserSettings.Volume, v => { UserSettings.Volume = v; UserSettings.ApplyVolume(); });
		ConnectSlider("Sensitivity", UserSettings.Sensitivity, v => UserSettings.Sensitivity = v);
		ConnectSlider("Effects", UserSettings.Effects, v => UserSettings.Effects = v);
		ConnectSlider("Motion", UserSettings.Motion, v => UserSettings.Motion = v);
		ConnectSlider("Gore", UserSettings.Gore, v => UserSettings.Gore = v);
		ConnectSlider("TextScale", UserSettings.TextScale, v => UserSettings.TextScale = v);
		ConnectSlider("Scares", UserSettings.Scares, v => UserSettings.Scares = v);
	}
	void ConnectSlider(string name, float initial, Action<float> set)
	{
		var slider = _settings.GetNode<HSlider>(name + "/Slider");
		slider.Value = initial;
		slider.ValueChanged += v => { set((float)v); UserSettings.Save(); SettingsChanged?.Invoke(); };
	}
	public void ShowMenu(bool inRun, bool checkpoint)
	{
		_inRun = inRun;
		_root.Show();
		_settings.Hide();
		_resume.Visible = inRun;
		_continue.Disabled = !checkpoint;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		if (inRun) _resume.GrabFocus();
		else if (checkpoint) _continue.GrabFocus();
		else GetNode<Button>("Root/Layout/Scroll/Controls/NewGame").GrabFocus();
	}
	public void HideMenu()
	{
		_root.Hide();
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}
	public override void _UnhandledInput(InputEvent input)
	{
		if (Open && _inRun && input.IsActionPressed("ui_cancel"))
		{
			ResumeGame?.Invoke();
			GetViewport().SetInputAsHandled();
		}
	}
}

