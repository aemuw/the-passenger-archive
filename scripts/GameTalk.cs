using System;
using System.Collections.Generic;
using Godot;

namespace PassengerArchive;

/// <summary>
/// Conversations. A <see cref="Convo"/> is a graph of <see cref="DNode"/>s: lines (E / click / Space advances),
/// then options (1–6, W/S + E, or click), which set flags and decide who says what next.
/// What you ask changes what people tell you — and some answers are kept for the whole run.
/// </summary>
public partial class Game
{
	Convo _convo;
	string _ownerId;
	Func<string> _ownerName;
	DNode _node;
	int _lineI;
	bool _inTalk, _choosing, _heroLine, _clickPending;
	List<Opt> _opts = new();
	int _sel;
	Opt _picked;
	Action _talkEnd;

	public bool InTalk => _inTalk;

	/// <summary>True while this person's line is still being "spoken" (typed out) — they nod and gesture.</summary>
	public bool NpcSpeakingNow(string id) => _inTalk && _ownerId == id && !_heroLine && !_choosing && Hud.IsTyping;

	/// <summary>Ambient muttering is allowed only when nothing else is going on.</summary>
	public bool BarksAllowed => _mode == Mode.Carriage && !_busy && !_dead && !_inTalk && _script == null;

	void ResetTalk()
	{
		_inTalk = false;
		_choosing = false;
		_heroLine = false;
		_clickPending = false;
		_convo = null;
		_node = null;
		_talkEnd = null;
		_opts = new List<Opt>();
	}

	/// <param name="ownerId">person id (for the "know" effect)</param>
	/// <param name="name">current display name — "Незнайомець …" until the name is learned</param>
	/// <param name="face">the view turns towards this point</param>
	/// <param name="force">start at this node instead of the entry that matches the flags</param>
	/// <param name="onEnd">called when the conversation closes</param>
	public void StartTalk(Convo convo, string ownerId, Func<string> name, Vector3? face = null, string force = null, Action onEnd = null)
	{
		if (_inTalk || convo == null || _dead) return;
		_convo = convo;
		_ownerId = ownerId;
		_ownerName = name;
		_talkEnd = onEnd;
		_inTalk = true;
		_choosing = false;
		_heroLine = false;
		_clickPending = false;
		_player.InputEnabled = false;
		if (face.HasValue) _player.FaceTowards(face.Value);
		Hud.SetPrompt(null);

		string start = force;
		if (start == null)
			foreach (var e in convo.Entries)
				if (Check(e.Need, e.Unless)) start = e.Node;    // the last matching entry wins
		if (start == null) { EndTalk(); return; }
		EnterNode(start);
	}

	void EnterNode(string id)
	{
		_node = _convo?.Find(id);
		if (_node == null) { EndTalk(); return; }
		ApplySet(_node.Set);
		_lineI = 0;
		if (_node.Say.Length == 0) FinishNode(); else ShowNodeLine();
	}

	void ShowNodeLine()
	{
		var l = _node.Say[_lineI];
		string who = l.Speaker != null ? ResolveSpeaker(l.Speaker) : (_ownerName?.Invoke() ?? "");
		Hud.ShowDialogue(who, l.Text, sticky: true);
		ApplyFx(l.Fx);
	}

	void AdvanceTalk()
	{
		if (_choosing) return;
		if (Hud.IsTyping) { Hud.FinishTyping(); return; }
		if (_heroLine) { _heroLine = false; AfterHeroLine(); return; }
		_lineI++;
		if (_lineI < _node.Say.Length) ShowNodeLine(); else FinishNode();
	}

	void FinishNode()
	{
		if (RunFx(_node.Fx)) return;

		var visible = new List<Opt>();
		if (_node.Opts != null)
			foreach (var o in _node.Opts)
				if (Check(o.Need, o.Unless)) visible.Add(o);

		if (visible.Count > 0)
		{
			_opts = visible;
			_sel = 0;
			_choosing = true;
			var texts = new string[visible.Count];
			for (int i = 0; i < texts.Length; i++) texts[i] = visible[i].Text;
			Hud.ShowChoices(texts, 0);
			return;
		}

		string next = (_node.IfTo != null && Check(_node.IfNeed, null)) ? _node.IfTo : _node.Next;
		if (next != null) EnterNode(next); else EndTalk();
	}

	void Choose(int i)
	{
		_picked = _opts[i];
		_choosing = false;
		Hud.HideChoices();
		ApplySet(_picked.Set);
		Hud.ShowDialogue("Ти", _picked.Text, sticky: true);   // the hero says it out loud, then they answer
		_heroLine = true;
	}

	void AfterHeroLine()
	{
		var o = _picked;
		if (RunFx(o.Fx)) return;
		if (o.To != null) EnterNode(o.To); else EndTalk();
	}

	static bool IsEndingFx(string fx) =>
		fx.StartsWith("memory:") || fx == "memend" || fx == "kill" || fx == "violate" || fx == "walkgo" || fx == "callend";

	/// <summary>Runs effects. Effects that take the player somewhere else close the conversation first. Returns true if it did.</summary>
	bool RunFx(string fx)
	{
		if (string.IsNullOrEmpty(fx)) return false;
		foreach (var raw in fx.Split(';'))
		{
			var part = raw.Trim();
			if (IsEndingFx(part))
			{
				EndTalk();
				ApplyFx(part);
				return true;
			}
			ApplyFx(part);
		}
		return false;
	}

	void EndTalk()
	{
		_inTalk = false;
		_choosing = false;
		_heroLine = false;
		_convo = null;
		_node = null;
		Hud.HideDialogue();
		var cb = _talkEnd;
		_talkEnd = null;
		if (!_dead && !_busy && !Hud.BookOpen) _player.InputEnabled = true;
		cb?.Invoke();
	}

	void TickTalk()
	{
		bool confirm = Input.IsActionJustPressed("interact") || Input.IsActionJustPressed("confirm") || _clickPending;
		_clickPending = false;

		if (_choosing)
		{
			int n = _opts.Count;
			if (Input.IsActionJustPressed("move_forward")) { _sel = (_sel + n - 1) % n; Hud.SetChoice(_sel); }
			else if (Input.IsActionJustPressed("move_back")) { _sel = (_sel + 1) % n; Hud.SetChoice(_sel); }

			for (int k = 0; k < Math.Min(n, 6); k++)
				if (Input.IsActionJustPressed("opt" + (k + 1))) { Choose(k); return; }

			if (confirm) Choose(_sel);
			return;
		}

		if (confirm) AdvanceTalk();
	}

}
