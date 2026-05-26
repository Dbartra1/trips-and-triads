using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.UI
{
    public partial class CardNode : Control
    {
        [Export] public Panel       Background      { get; set; }
        [Export] public TextureRect CardArt         { get; set; }
        [Export] public Label       LabelTop        { get; set; }
        [Export] public Label       LabelRight      { get; set; }
        [Export] public Label       LabelBottom     { get; set; }
        [Export] public Label       LabelLeft       { get; set; }
        [Export] public Label       LabelName       { get; set; }
        [Export] public Panel       SelectionBorder { get; set; }

        private CardInstance _cardInstance;

        // Set to true by PreMatchScreen for the interim hero so border shows orange.
        public bool IsInterim { get; set; } = false;

        private static readonly Color P1Color      = new Color("4a90d9");
        private static readonly Color P2Color      = new Color("d94a4a");
        private static readonly Color SelectedTint = new Color(1.25f, 1.25f, 1.25f);

        // ── Tier border colors — cyberpunk loot rarity ─────────────────────────
        private static Color TierBorderColor(Tier tier, bool isInterim) => tier switch
        {
            Tier.Hero    => isInterim ? new Color("ff6d00") : new Color("ea00ff"),
            Tier.TopTier => new Color("ffd600"),
            Tier.Pro     => new Color("00e5ff"),
            _            => new Color("78909c"),   // Street
        };

        private static Color FactionColor(Faction faction) => faction switch
        {
            Faction.Ascendant   => new Color("1a2a1a"),
            Faction.Razorkin    => new Color("2a0a0a"),
            Faction.Ghostwire   => new Color("0a1a2a"),
            Faction.Commons     => new Color("1a1a0a"),
            Faction.Effigy      => new Color("1a0a1a"),
            Faction.Lacquer     => new Color("1a1510"),
            Faction.HollowChoir => new Color("050510"),
            _                   => new Color("0f0f0f"),
        };

        public override void _Ready()
        {
            // Fallback resolution — if the Inspector didn't wire these,
            // find them by node name so the card works without manual wiring.
            Background      ??= GetNodeOrNull<Panel>("Panel");
            CardArt         ??= GetNodeOrNull<TextureRect>("CardArt");
            LabelTop        ??= GetNodeOrNull<Label>("LabelTop");
            LabelRight      ??= GetNodeOrNull<Label>("LabelRight");
            LabelBottom     ??= GetNodeOrNull<Label>("LabelBottom");
            LabelLeft       ??= GetNodeOrNull<Label>("LabelLeft");
            LabelName       ??= GetNodeOrNull<Label>("LabelName");
            SelectionBorder ??= GetNodeOrNull<Panel>("SelectionBorder");
        }

        public void Initialize(CardInstance instance)
        {
            _cardInstance = instance;

            // Resolve nodes now if _Ready hasn't fired yet (e.g. instantiated but not in tree)
            Background      ??= GetNodeOrNull<Panel>("Panel");
            LabelTop        ??= GetNodeOrNull<Label>("LabelTop");
            LabelRight      ??= GetNodeOrNull<Label>("LabelRight");
            LabelBottom     ??= GetNodeOrNull<Label>("LabelBottom");
            LabelLeft       ??= GetNodeOrNull<Label>("LabelLeft");
            LabelName       ??= GetNodeOrNull<Label>("LabelName");
            SelectionBorder ??= GetNodeOrNull<Panel>("SelectionBorder");

            if (Background != null)
            {
                Background.AnchorLeft   = 0;
                Background.AnchorTop    = 0;
                Background.AnchorRight  = 1;
                Background.AnchorBottom = 1;
                Background.OffsetLeft   = 0;
                Background.OffsetTop    = 0;
                Background.OffsetRight  = 0;
                Background.OffsetBottom = 0;
                Background.MouseFilter  = MouseFilterEnum.Ignore;
                Background.SelfModulate = FactionColor(instance.Data.Faction);
            }

            if (LabelTop != null)
            {
                LabelTop.AnchorLeft   = 0.5f; LabelTop.AnchorRight  = 0.5f;
                LabelTop.AnchorTop    = 0f;   LabelTop.AnchorBottom = 0f;
                LabelTop.OffsetLeft   = -10;  LabelTop.OffsetRight  = 10;
                LabelTop.OffsetTop    = 4;    LabelTop.OffsetBottom = 24;
                LabelTop.HorizontalAlignment = HorizontalAlignment.Center;
            }

            if (LabelBottom != null)
            {
                LabelBottom.AnchorLeft   = 0.5f; LabelBottom.AnchorRight  = 0.5f;
                LabelBottom.AnchorTop    = 1f;   LabelBottom.AnchorBottom = 1f;
                LabelBottom.OffsetLeft   = -10;  LabelBottom.OffsetRight  = 10;
                LabelBottom.OffsetTop    = -24;  LabelBottom.OffsetBottom = -4;
                LabelBottom.HorizontalAlignment = HorizontalAlignment.Center;
            }

            if (LabelLeft != null)
            {
                LabelLeft.AnchorLeft   = 0f;   LabelLeft.AnchorRight  = 0f;
                LabelLeft.AnchorTop    = 0.5f; LabelLeft.AnchorBottom = 0.5f;
                LabelLeft.OffsetLeft   = 4;    LabelLeft.OffsetRight  = 24;
                LabelLeft.OffsetTop    = -10;  LabelLeft.OffsetBottom = 10;
                LabelLeft.HorizontalAlignment = HorizontalAlignment.Center;
            }

            if (LabelRight != null)
            {
                LabelRight.AnchorLeft   = 1f;   LabelRight.AnchorRight  = 1f;
                LabelRight.AnchorTop    = 0.5f; LabelRight.AnchorBottom = 0.5f;
                LabelRight.OffsetLeft   = -24;  LabelRight.OffsetRight  = -4;
                LabelRight.OffsetTop    = -10;  LabelRight.OffsetBottom = 10;
                LabelRight.HorizontalAlignment = HorizontalAlignment.Center;
            }

            if (LabelName != null)
            {
                LabelName.AnchorLeft   = 0f;   LabelName.AnchorRight  = 1f;
                LabelName.AnchorTop    = 0.5f; LabelName.AnchorBottom = 0.5f;
                LabelName.OffsetLeft   = 4;    LabelName.OffsetRight  = -4;
                LabelName.OffsetTop    = -10;  LabelName.OffsetBottom = 10;
                LabelName.HorizontalAlignment = HorizontalAlignment.Center;
                LabelName.VerticalAlignment   = VerticalAlignment.Center;
                LabelName.AutowrapMode        = TextServer.AutowrapMode.WordSmart;
                LabelName.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.55f));
            }

            ApplyTierBorder(instance.Data.Tier);
            Refresh();
        }

        private void ApplyTierBorder(Tier tier)
        {
            var borderColor = TierBorderColor(tier, IsInterim);
            var glowColor   = new Color(borderColor.R, borderColor.G, borderColor.B, 0.35f);

            // Outer glow layer — slightly larger, translucent
            var glowStyle = new StyleBoxFlat();
            glowStyle.BgColor           = new Color(0, 0, 0, 0);
            glowStyle.BorderWidthLeft   = 3;
            glowStyle.BorderWidthTop    = 3;
            glowStyle.BorderWidthRight  = 3;
            glowStyle.BorderWidthBottom = 3;
            glowStyle.BorderColor       = glowColor;
            glowStyle.SetCornerRadiusAll(4);
            glowStyle.ShadowColor  = glowColor;
            glowStyle.ShadowSize   = 6;
            glowStyle.ShadowOffset = Vector2.Zero;

            var glowPanel = new Panel();
            glowPanel.MouseFilter = MouseFilterEnum.Ignore;
            glowPanel.SetAnchorsPreset(LayoutPreset.FullRect);
            glowPanel.AddThemeStyleboxOverride("panel", glowStyle);
            AddChild(glowPanel);
            MoveChild(glowPanel, 1); // Just above Background, behind everything else

            // Inner crisp border layer
            var borderStyle = new StyleBoxFlat();
            borderStyle.BgColor           = new Color(0, 0, 0, 0);
            borderStyle.BorderWidthLeft   = 2;
            borderStyle.BorderWidthTop    = 2;
            borderStyle.BorderWidthRight  = 2;
            borderStyle.BorderWidthBottom = 2;
            borderStyle.BorderColor       = borderColor;
            borderStyle.SetCornerRadiusAll(3);

            var borderPanel = new Panel();
            borderPanel.MouseFilter = MouseFilterEnum.Ignore;
            borderPanel.SetAnchorsPreset(LayoutPreset.FullRect);
            borderPanel.AddThemeStyleboxOverride("panel", borderStyle);
            AddChild(borderPanel);
            MoveChild(borderPanel, 2); // Above glow, below labels
        }

        public void Refresh()
        {
            if (_cardInstance == null) return;

            // Re-resolve in case Refresh is called before _Ready fires
            LabelTop        ??= GetNodeOrNull<Label>("LabelTop");
            LabelRight      ??= GetNodeOrNull<Label>("LabelRight");
            LabelBottom     ??= GetNodeOrNull<Label>("LabelBottom");
            LabelLeft       ??= GetNodeOrNull<Label>("LabelLeft");
            LabelName       ??= GetNodeOrNull<Label>("LabelName");

            // Read edges from the instance (respects overrides from Vesna/Sumi/Lethe).
            // Name comes from CardData — it never changes.
            if (LabelTop    != null) LabelTop.Text    = _cardInstance.GetValue(TripsAndTriads.Core.Direction.Top).ToString();
            if (LabelRight  != null) LabelRight.Text  = _cardInstance.GetValue(TripsAndTriads.Core.Direction.Right).ToString();
            if (LabelBottom != null) LabelBottom.Text = _cardInstance.GetValue(TripsAndTriads.Core.Direction.Bottom).ToString();
            if (LabelLeft   != null) LabelLeft.Text   = _cardInstance.GetValue(TripsAndTriads.Core.Direction.Left).ToString();
            if (LabelName   != null) LabelName.Text   = _cardInstance.Data.Name;

            SetOwnerColor(_cardInstance.OwnerId);
        }

        public void SetOwnerColor(int ownerId)
        {
            Modulate = ownerId == 1 ? P1Color : P2Color;
        }

        public void SetSelected(bool selected)
        {
            SelectionBorder ??= GetNodeOrNull<Panel>("SelectionBorder");

            if (SelectionBorder != null)
                SelectionBorder.Visible = selected;

            if (selected)
                Modulate = (_cardInstance?.OwnerId == 1 ? P1Color : P2Color) * SelectedTint;
            else
                SetOwnerColor(_cardInstance?.OwnerId ?? 1);
        }

        public CardInstance GetCardInstance() => _cardInstance;
    }
}