using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.UI
{
	/// <summary>
	/// "The Wire" — a scrolling intercept-comms ticker at the bottom of the board.
	///
	/// Each capture event produces a short entry in monospace hacker-terminal style.
	/// New entries are appended to a running list; all entries scroll left as new
	/// ones arrive. The oldest entries fade out automatically via a Tween.
	///
	/// Format:  ◈ VESNA → CROW LORN  [20>13 L]  ·  aura +4  ◈
	///          ◈ HANDSHAKE — VERITY / RYN MARSH  ◈
	/// </summary>
	public partial class KillFeedNode : Control
	{
		// ── Config ────────────────────────────────────────────────────────────────

		/// <summary>Maximum number of entries visible at once in the ticker line.</summary>
		private const int MaxVisible = 6;

		/// <summary>Seconds each entry stays at full opacity before fading.</summary>
		private const float EntryLifetime = 6.0f;

		/// <summary>Separator between entries in the scrolling line.</summary>
		private const string Separator = "  ◈  ";

		// ── State ─────────────────────────────────────────────────────────────────

		private readonly Queue<string> _entries = new();
		private Label _tickerLabel;
		private Panel _background;

		// ── Faction color map (matches card colors) ───────────────────────────────
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
			// Background panel — dark, semi-transparent
			_background = new Panel();
			var style = new StyleBoxFlat();
			style.BgColor = new Color(0.03f, 0.03f, 0.06f, 0.82f);
			style.BorderWidthTop = 1;
			style.BorderColor    = new Color(0.15f, 0.6f, 0.5f, 0.6f); // dim teal border
			_background.AddThemeStyleboxOverride("panel", style);
			_background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			_background.MouseFilter = Control.MouseFilterEnum.Ignore;
			AddChild(_background);

			// Ticker label — fills the panel
			_tickerLabel = new Label();
			_tickerLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			_tickerLabel.OffsetLeft  = 12;
			_tickerLabel.OffsetRight = -12;
			_tickerLabel.HorizontalAlignment = HorizontalAlignment.Left;
			_tickerLabel.VerticalAlignment   = VerticalAlignment.Center;
			_tickerLabel.ClipContents        = true;
			_tickerLabel.AddThemeColorOverride("font_color",
				new Color(0.55f, 0.95f, 0.75f, 1f)); // monochrome green-teal
			_tickerLabel.AddThemeFontSizeOverride("font_size", 13);
			_tickerLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
			AddChild(_tickerLabel);

			// Start empty
			_tickerLabel.Text = "◈ WIRE ACTIVE — AWAITING INTERCEPT";
		}

		// ── Public API ────────────────────────────────────────────────────────────

		/// <summary>
		/// Push all events from a PlayCard turn onto the feed.
		/// </summary>
		public void PushEvents(List<CaptureEvent> events)
		{
			if (events == null || events.Count == 0) return;

			foreach (var ev in events)
				_entries.Enqueue(FormatEvent(ev));

			// Keep the queue bounded
			while (_entries.Count > MaxVisible * 2)
				_entries.Dequeue();

			RefreshTicker();
			ScheduleFade();
		}

		// ── Formatting ────────────────────────────────────────────────────────────

		private static string FormatEvent(CaptureEvent ev)
		{
			string capturingTag = FactionTag.TryGetValue(ev.CapturingFaction, out var ct) ? ct : "";
			string capturedTag  = FactionTag.TryGetValue(ev.CapturedFaction,  out var dt) ? dt : "";

			// Base capture line
			string edgeLabel = ev.Edge switch
			{
				Direction.Top    => "T",
				Direction.Right  => "R",
				Direction.Bottom => "B",
				Direction.Left   => "L",
				_                => "?"
			};

			// Protocol-only capture (no attack/defend values)
			if (!string.IsNullOrEmpty(ev.ProtocolNote) && ev.AttackVal == 0 && ev.DefendVal == 0)
				return $"◈ {ev.ProtocolNote.ToUpper()} — " +
				       $"{ev.CapturingName.ToUpper()}{capturingTag} / " +
				       $"{ev.CapturedName.ToUpper()}{capturedTag}";

			// Normal capture — with optional domain note
			string scores = $"[{ev.AttackVal}>{ev.DefendVal} {edgeLabel}]";
			string mods   = "";
			if (!string.IsNullOrEmpty(ev.DomainNote))
				mods += $"  ·  {ev.DomainNote.ToUpper()}";
			if (!string.IsNullOrEmpty(ev.ProtocolNote))
				mods += $"  ·  {ev.ProtocolNote.ToUpper()}";

			return $"◈ {ev.CapturingName.ToUpper()}{capturingTag} → " +
			       $"{ev.CapturedName.ToUpper()}{capturedTag}  {scores}{mods}";
		}

		// ── Display ───────────────────────────────────────────────────────────────

		private void RefreshTicker()
		{
			if (_tickerLabel == null) return;

			var parts = new List<string>(_entries);
			// Show most recent MaxVisible entries, newest at the right
			int start = System.Math.Max(0, parts.Count - MaxVisible);
			var visible = parts.GetRange(start, parts.Count - start);
			_tickerLabel.Text = string.Join(Separator, visible);
		}

		private void ScheduleFade()
		{
			// After EntryLifetime seconds, fade the oldest entry out and refresh.
			var timer = GetTree().CreateTimer(EntryLifetime);
			timer.Timeout += () =>
			{
				if (_entries.Count > 0) _entries.Dequeue();
				if (_entries.Count == 0)
					_tickerLabel.Text = "◈ WIRE ACTIVE — AWAITING INTERCEPT";
				else
					RefreshTicker();
			};
		}
	}
}