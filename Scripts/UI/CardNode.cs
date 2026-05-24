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

        private static readonly Color P1Color      = new Color("4a90d9");
        private static readonly Color P2Color      = new Color("d94a4a");
        private static readonly Color SelectedTint = new Color(1.25f, 1.25f, 1.25f);

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

            Refresh();
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

            var data = _cardInstance.Data;

            if (LabelTop    != null) LabelTop.Text    = data.Top.ToString();
            if (LabelRight  != null) LabelRight.Text  = data.Right.ToString();
            if (LabelBottom != null) LabelBottom.Text = data.Bottom.ToString();
            if (LabelLeft   != null) LabelLeft.Text   = data.Left.ToString();
            if (LabelName   != null) LabelName.Text   = data.Name;

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