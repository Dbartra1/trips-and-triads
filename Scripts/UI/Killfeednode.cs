using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.UI
{
	/// <summary>
	/// "The Wire" — a scrolling intercept-comms log at the bottom of the board.
	///
	/// Entries stack vertically, newest at the bottom. Each entry lives for
	/// EntryLifetime seconds before fading. The panel is tall enough to show
	/// three entries at once so fast turns don't lose events.
	///
	/// Format:  ◈ VESNA[HCH] → CROW LORN[RZK]  [14>8 L]  ·  AURA +4
	///          ◈ HANDSHAKE — VERITY[EFF] / RYN MARSH[GWI]
	/// </summary>
	public partial class KillFeedNode : Control
	{
		// ── Config ────────────────────────────────────────────────────────────────

		/// <summary>Maximum entries kept in memory.</summary>
		private const int MaxEntries = 12;

		/// <summary>Maximum entries shown in the panel at once.</summary>
		private const int MaxVisible = 4;

		/// <summary>Seconds each entry stays visible before it is dropped.</summary>
		private const float EntryLifetime = 8.0f;

		// ── State ─────────────────────────────────────────────────────────────────

		private readonly List<string> _entries    = new();
		private VBoxContainer         _linesBox;
		private Panel                 _background;

		// ── Faction tags ──────────────────────────────────────────────────────────
		private static readonly Dictionary<Faction, string> FactionTag = new()
		{
			{ Faction.Ascendant,   "[ASC]"  },
			{ Faction.Razorkin,    "[RZK]"  },
			{ Faction.Ghostwire,   "[GWI]"  },
			{ Faction.Commons,     "[COM]"  },
			{ Faction.Effigy,      "[EFF]"  },
			{ Faction.Lacquer,     "[LAC]"  },
			{ Faction.HollowChoir, "[HCH]"  },
			{ Faction.None,        "[FREE]" },
		};

		// ── Lifecycle ─────────────────────────────────────────────────────────────

		public override void _Ready()
		{
			// Background panel
			_background = new Panel();
			var style   = new StyleBoxFlat();
			style.BgColor          = new Color(0.03f, 0.03f, 0.06f, 0.85f);
			style.BorderWidthTop   = 1;
			style.BorderColor      = new Color(0.15f, 0.6f, 0.5f, 0.6f);
			_background.AddThemeStyleboxOverride("panel", style);
			_background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			_background.MouseFilter = Control.MouseFilterEnum.Ignore;
			AddChild(_background);

			// Vertical stack of label lines — one per entry
			_linesBox = new VBoxContainer();
			_linesBox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			_linesBox.OffsetLeft    = 12;
			_linesBox.OffsetRight   = -12;
			_linesBox.OffsetTop     = 8;
			_linesBox.OffsetBottom  = -8;
			_linesBox.MouseFilter   = Control.MouseFilterEnum.Ignore;
			_linesBox.AddThemeConstantOverride("separation", 5);
			AddChild(_linesBox);

			// Idle message — single centered label
			PushIdleMessage();
		}

		// ── Public API ────────────────────────────────────────────────────────────

		public void PushEvents(List<CaptureEvent> events)
		{
			if (events == null || events.Count == 0) return;

			foreach (var ev in events)
				_entries.Add(FormatEvent(ev));

			while (_entries.Count > MaxEntries)
				_entries.RemoveAt(0);

			RebuildLines();
			ScheduleFade();
		}

		// ── Display ───────────────────────────────────────────────────────────────

		private void RebuildLines()
		{
			foreach (var child in _linesBox.GetChildren())
				child.QueueFree();

			if (_entries.Count == 0) { PushIdleMessage(); return; }

			// Show the most recent MaxVisible entries, oldest at top
			int start = System.Math.Max(0, _entries.Count - MaxVisible);
			for (int i = start; i < _entries.Count; i++)
			{
				// Older entries are dimmer — fades from 0.45 to 1.0 across the window
				float alpha = 0.45f + 0.55f * (float)(i - start) / System.Math.Max(1, MaxVisible - 1);

				var lbl = new Label();
				lbl.Text          = _entries[i];
				lbl.Modulate      = new Color(0.55f, 0.95f, 0.75f, alpha);
				lbl.AddThemeFontSizeOverride("font_size", 13);
				lbl.MouseFilter   = Control.MouseFilterEnum.Ignore;
				lbl.ClipContents  = true;
				_linesBox.AddChild(lbl);
			}
		}

		private void PushIdleMessage()
		{
			var lbl = new Label();
			lbl.Text        = "◈  WIRE ACTIVE  —  AWAITING INTERCEPT";
			lbl.Modulate    = new Color(0.55f, 0.95f, 0.75f, 0.4f);
			lbl.AddThemeFontSizeOverride("font_size", 13);
			lbl.MouseFilter = Control.MouseFilterEnum.Ignore;
			_linesBox.AddChild(lbl);
		}

		private void ScheduleFade()
		{
			var timer = GetTree().CreateTimer(EntryLifetime);
			timer.Timeout += () =>
			{
				if (_entries.Count > 0) _entries.RemoveAt(0);
				RebuildLines();
			};
		}

		// ── Formatting ────────────────────────────────────────────────────────────

		private static string FormatEvent(CaptureEvent ev)
		{
			string ct = FactionTag.TryGetValue(ev.CapturingFaction, out var c1) ? c1 : "";
			string dt = FactionTag.TryGetValue(ev.CapturedFaction,  out var c2) ? c2 : "";

			string edgeLbl = ev.Edge switch
			{
				Direction.Top    => "T",
				Direction.Right  => "R",
				Direction.Bottom => "B",
				Direction.Left   => "L",
				_                => "?"
			};

			// Protocol-only (Handshake / Tally — no edge values)
			if (!string.IsNullOrEmpty(ev.ProtocolNote) && ev.AttackVal == 0 && ev.DefendVal == 0)
				return $"◈  {ev.ProtocolNote.ToUpper()}  —  " +
				       $"{ev.CapturingName.ToUpper()}{ct} / {ev.CapturedName.ToUpper()}{dt}";

			string scores = $"[{ev.AttackVal}>{ev.DefendVal} {edgeLbl}]";
			string mods   = "";
			if (!string.IsNullOrEmpty(ev.DomainNote))   mods += $"  ·  {ev.DomainNote.ToUpper()}";
			if (!string.IsNullOrEmpty(ev.ProtocolNote)) mods += $"  ·  {ev.ProtocolNote.ToUpper()}";

			return $"◈  {ev.CapturingName.ToUpper()}{ct}  →  {ev.CapturedName.ToUpper()}{dt}  {scores}{mods}";
		}
	}
}