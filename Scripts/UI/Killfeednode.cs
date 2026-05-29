using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.UI
{
	/// <summary>
	/// "The Wire" — a scrollable intercept-comms log at the bottom of the board.
	///
	/// Entries never expire — the full match history is kept and scrollable.
	/// Newest entries appear at the bottom. The panel auto-scrolls to the
	/// latest entry after each turn.
	/// </summary>
	public partial class KillFeedNode : Control
	{
		private ScrollContainer _scroll;
		private VBoxContainer   _linesBox;
		private Panel           _background;

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

		public override void _Ready()
		{
			// Background
			_background = new Panel();
			var style   = new StyleBoxFlat();
			style.BgColor        = new Color(0.03f, 0.03f, 0.06f, 0.85f);
			style.BorderWidthTop = 1;
			style.BorderColor    = new Color(0.15f, 0.6f, 0.5f, 0.6f);
			_background.AddThemeStyleboxOverride("panel", style);
			_background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			_background.MouseFilter = Control.MouseFilterEnum.Ignore;
			AddChild(_background);

			// ScrollContainer fills the panel
			_scroll = new ScrollContainer();
			_scroll.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			_scroll.OffsetLeft   = 8;
			_scroll.OffsetRight  = -8;
			_scroll.OffsetTop    = 6;
			_scroll.OffsetBottom = -6;
			// Show vertical scrollbar; hide horizontal
			_scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
			_scroll.VerticalScrollMode   = ScrollContainer.ScrollMode.ShowAlways;
			AddChild(_scroll);

			// Entry stack inside the scroll
			_linesBox = new VBoxContainer();
			_linesBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			_linesBox.AddThemeConstantOverride("separation", 5);
			_scroll.AddChild(_linesBox);

			PushIdleMessage();
		}

		// ── Public API ────────────────────────────────────────────────────────────

		public void PushEvents(List<CaptureEvent> events)
		{
			if (events == null || events.Count == 0) return;

			// Remove idle message on first real entry
			if (_linesBox.GetChildCount() == 1 &&
			    _linesBox.GetChild(0) is Label first &&
			    first.Text.Contains("AWAITING INTERCEPT"))
				first.QueueFree();

			foreach (var ev in events)
				AppendLine(FormatEvent(ev), alpha: 1.0f);

			// Auto-scroll to bottom after Godot has laid out the new nodes
			CallDeferred(MethodName.ScrollToBottom);
		}

		// ── Display ───────────────────────────────────────────────────────────────

		private void AppendLine(string text, float alpha)
		{
			var lbl = new Label();
			lbl.Text          = text;
			lbl.Modulate      = new Color(0.55f, 0.95f, 0.75f, alpha);
			lbl.AddThemeFontSizeOverride("font_size", 13);
			lbl.MouseFilter   = Control.MouseFilterEnum.Ignore;
			lbl.AutowrapMode  = TextServer.AutowrapMode.Off;
			_linesBox.AddChild(lbl);
		}

		private void PushIdleMessage()
		{
			AppendLine("◈  WIRE ACTIVE  —  AWAITING INTERCEPT", alpha: 0.4f);
		}

		private void ScrollToBottom()
		{
			_scroll.ScrollVertical = (int)_scroll.GetVScrollBar().MaxValue;
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