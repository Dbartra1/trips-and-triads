using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.UI
{
	/// <summary>
	/// "CITY SIGNAL" — Street Cred visualised as a five-column signal-strength
	/// meter. Each column represents one tier. Completed tiers glow. The active
	/// tier pulses and shows a scan line. Future tiers are dim silhouettes.
	///
	/// Designed to feel like a broadcast strength indicator on surveillance
	/// hardware — not a fantasy XP bar, not a health meter.
	///
	/// Add to a scene and call Refresh(credManager) after any cred change.
	/// </summary>
	public partial class CredBarNode : Control
	{
		// ── Layout constants ──────────────────────────────────────────────────────
		private const int ColCount    = 5;    // one per tier
		private const int ColWidth    = 40;
		private const int ColGap      = 6;
		private const int ColMaxH     = 48;   // tallest column (Legend)
		private const int ColMinH     = 24;   // shortest column (Nameless)
		private const int LabelH      = 18;
		private const int ScanBarH    = 3;
		private const int PanelPad    = 10;

		// ── Colors ────────────────────────────────────────────────────────────────
		private static readonly Color ColDone    = new Color("3ecdef");          // cyan  — completed tier
		private static readonly Color ColActive  = new Color("3ecdef");          // cyan  — active tier (pulsed)
		private static readonly Color ColFuture  = new Color(0.2f, 0.2f, 0.25f, 1f); // dark grey — locked
		private static readonly Color ScanColor  = new Color(1f, 1f, 1f, 0.7f); // bright white scan line
		private static readonly Color LabelDone  = new Color("3ecdef");
		private static readonly Color LabelActive = Colors.White;
		private static readonly Color LabelFuture = new Color(0.4f, 0.4f, 0.45f, 1f);
		private static readonly Color TierTextCol = Colors.White;
		private static readonly Color CredNumCol  = new Color(0.6f, 0.6f, 0.65f, 1f);

		// ── Tier metadata ─────────────────────────────────────────────────────────
		private static readonly string[] TierNames  = { "NMLS", "KNWN", "NAMD", "NOTO", "LGND" };
		private static readonly CredTier[] AllTiers =
		{
			CredTier.Nameless, CredTier.Known, CredTier.Named,
			CredTier.Notorious, CredTier.Legend
		};

		// ── State ─────────────────────────────────────────────────────────────────
		private CredManager _cred;
		private float       _scanOffset  = 0f;   // 0..1, moves across active column
		private float       _pulseAlpha  = 1f;
		private bool        _pulseDir    = false;

		// ── Child nodes ───────────────────────────────────────────────────────────
		private Panel         _background;
		private ColorRect[]   _colRects    = new ColorRect[ColCount];
		private ColorRect     _scanRect;
		private Label[]       _tierLabels  = new Label[ColCount];
		private Label         _tierNameLbl;
		private Label         _credNumLbl;

		// ── Lifecycle ─────────────────────────────────────────────────────────────

		public override void _Ready()
		{
			// Total width = 5 columns + 4 gaps + 2× padding
			float totalW = ColCount * ColWidth + (ColCount - 1) * ColGap + 2 * PanelPad;
			float totalH = ColMaxH + LabelH + PanelPad * 2 + 28; // +28 for tier readout
			CustomMinimumSize = new Vector2(totalW, totalH);

			// Background
			_background = new Panel();
			_background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			var bgStyle   = new StyleBoxFlat();
			bgStyle.BgColor = new Color(0.04f, 0.04f, 0.08f, 0.9f);
			bgStyle.BorderWidthTop = 1;
			bgStyle.BorderColor    = new Color(0.15f, 0.55f, 0.45f, 0.5f);
			bgStyle.SetCornerRadiusAll(4);
			_background.AddThemeStyleboxOverride("panel", bgStyle);
			_background.MouseFilter = MouseFilterEnum.Ignore;
			AddChild(_background);

			// Tier name + cred number readout (top of panel)
			_tierNameLbl = MakeLabel("", 15, TierTextCol);
			_tierNameLbl.Position = new Vector2(PanelPad, PanelPad - 2);
			AddChild(_tierNameLbl);

			_credNumLbl = MakeLabel("", 12, CredNumCol);
			_credNumLbl.Position = new Vector2(PanelPad, PanelPad + 16);
			AddChild(_credNumLbl);

			// Build the five columns + their labels
			float baseX = PanelPad;
			float bottomY = PanelPad + 28 + ColMaxH; // baseline for all columns

			for (int i = 0; i < ColCount; i++)
			{
				float x = baseX + i * (ColWidth + ColGap);
				float h = ColHeightForIndex(i);
				float y = bottomY - h;

				var col = new ColorRect();
				col.Position = new Vector2(x, y);
				col.Size     = new Vector2(ColWidth, h);
				col.Color    = ColFuture;
				AddChild(col);
				_colRects[i] = col;

				var lbl = MakeLabel(TierNames[i], 10, LabelFuture);
				lbl.Position                = new Vector2(x, bottomY + 2);
				lbl.Size                    = new Vector2(ColWidth, LabelH);
				lbl.HorizontalAlignment     = HorizontalAlignment.Center;
				AddChild(lbl);
				_tierLabels[i] = lbl;
			}

			// Scan line — child of the active column, repositioned in Refresh
			_scanRect = new ColorRect();
			_scanRect.Size    = new Vector2(ColWidth, ScanBarH);
			_scanRect.Color   = ScanColor;
			_scanRect.Visible = false;
			AddChild(_scanRect);

			// Default to Nameless with 0 cred
			Refresh(new CredManager());
		}

		public override void _Process(double delta)
		{
			if (_cred == null) return;

			// Scan line sweeps downward across active column
			_scanOffset += (float)delta * 1.4f;
			if (_scanOffset > 1f) _scanOffset = 0f;

			// Pulse active column alpha between 0.55 and 1.0
			float speed = 1.5f;
			if (_pulseDir) _pulseAlpha += (float)delta * speed;
			else           _pulseAlpha -= (float)delta * speed;
			_pulseAlpha = Mathf.Clamp(_pulseAlpha, 0.55f, 1.0f);
			if (_pulseAlpha >= 1.0f) _pulseDir = false;
			if (_pulseAlpha <= 0.55f) _pulseDir = true;

			ApplyAnimation();
		}

		// ── Public API ────────────────────────────────────────────────────────────

		/// <summary>Refresh the bar to match the given CredManager state.</summary>
		public void Refresh(CredManager cred)
		{
			_cred        = cred;
			_scanOffset  = 0f;
			_pulseAlpha  = 1f;
			_pulseDir    = false;

			int      credVal  = cred.Cred;
			CredTier tier     = cred.Tier;
			int      tierIdx  = (int)tier;
			int      floor    = CredManager.TierFloor(tier);
			int      ceiling  = CredManager.TierCeiling(tier);

			// Header readout
			if (_tierNameLbl != null)
				_tierNameLbl.Text = $"◈  {tier.ToString().ToUpper()}";
			if (_credNumLbl != null)
				_credNumLbl.Text  = $"{credVal} / {ceiling}  [{floor}–{ceiling}]";

			// Column colors and labels
			for (int i = 0; i < ColCount; i++)
			{
				if (_colRects[i] == null || _tierLabels[i] == null) continue;

				if (i < tierIdx)
				{
					// Completed tier — solid glow
					_colRects[i].Color   = ColDone;
					_tierLabels[i].AddThemeColorOverride("font_color", LabelDone);
				}
				else if (i == tierIdx)
				{
					// Active tier — partial fill + pulse (applied in _Process)
					_colRects[i].Color   = ColActive;
					_tierLabels[i].AddThemeColorOverride("font_color", LabelActive);

					// Partial fill: resize the column to show progress within tier
					float progress    = (float)(credVal - floor) / (ceiling - floor + 1);
					float fullH       = ColHeightForIndex(i);
					float filledH     = Mathf.Max(4f, fullH * progress);
					float baseBottomY = PanelPad + 28 + ColMaxH;
					float x           = PanelPad + i * (ColWidth + ColGap);
					_colRects[i].Position = new Vector2(x, baseBottomY - filledH);
					_colRects[i].Size     = new Vector2(ColWidth, filledH);

					// Scan line on active column
					if (_scanRect != null)
					{
						_scanRect.Visible  = true;
						_scanRect.Size     = new Vector2(ColWidth, ScanBarH);
						float scanY        = _colRects[i].Position.Y +
						                    _scanOffset * (_colRects[i].Size.Y - ScanBarH);
						_scanRect.Position = new Vector2(x, scanY);
					}
				}
				else
				{
					// Future tier — dim silhouette at full column height
					float fullH       = ColHeightForIndex(i);
					float baseBottomY = PanelPad + 28 + ColMaxH;
					float x           = PanelPad + i * (ColWidth + ColGap);
					_colRects[i].Position = new Vector2(x, baseBottomY - fullH);
					_colRects[i].Size     = new Vector2(ColWidth, fullH);
					_colRects[i].Color    = ColFuture;
					_tierLabels[i].AddThemeColorOverride("font_color", LabelFuture);
				}
			}
		}

		// ── Animation tick ────────────────────────────────────────────────────────

		private void ApplyAnimation()
		{
			if (_cred == null) return;
			int tierIdx = (int)_cred.Tier;

			// Pulse the active column
			if (tierIdx < ColCount && _colRects[tierIdx] != null)
				_colRects[tierIdx].Modulate = new Color(1f, 1f, 1f, _pulseAlpha);

			// Move the scan line
			if (_scanRect != null && _scanRect.Visible && tierIdx < ColCount)
			{
				var colRect = _colRects[tierIdx];
				if (colRect != null)
				{
					float scanY        = colRect.Position.Y +
					                    _scanOffset * (colRect.Size.Y - ScanBarH);
					_scanRect.Position = new Vector2(_scanRect.Position.X, scanY);
				}
			}
		}

		// ── Helpers ───────────────────────────────────────────────────────────────

		/// Column heights scale linearly from ColMinH (Nameless) to ColMaxH (Legend)
		private static float ColHeightForIndex(int i) =>
			ColMinH + (ColMaxH - ColMinH) * i / (float)(ColCount - 1);

		private static Label MakeLabel(string text, int fontSize, Color color)
		{
			var lbl = new Label();
			lbl.Text        = text;
			lbl.MouseFilter = MouseFilterEnum.Ignore;
			lbl.AddThemeFontSizeOverride("font_size", fontSize);
			lbl.AddThemeColorOverride("font_color", color);
			return lbl;
		}
	}
}
