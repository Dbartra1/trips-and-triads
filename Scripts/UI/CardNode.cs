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

        private static readonly Color P1Color      = new Color("3ecdef"); // Player — cyan
        private static readonly Color P2Color      = new Color("fd1d75"); // Enemy  — magenta

        public override void _Ready()
        {
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
                Background.SelfModulate = Colors.White;
            }

            // All labels white — no tinting ever
            var white = Colors.White;
            if (LabelTop    != null) LabelTop.AddThemeColorOverride("font_color",    white);
            if (LabelRight  != null) LabelRight.AddThemeColorOverride("font_color",  white);
            if (LabelBottom != null) LabelBottom.AddThemeColorOverride("font_color", white);
            if (LabelLeft   != null) LabelLeft.AddThemeColorOverride("font_color",   white);

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
                LabelName.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.7f));
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_cardInstance == null) return;

            LabelTop        ??= GetNodeOrNull<Label>("LabelTop");
            LabelRight      ??= GetNodeOrNull<Label>("LabelRight");
            LabelBottom     ??= GetNodeOrNull<Label>("LabelBottom");
            LabelLeft       ??= GetNodeOrNull<Label>("LabelLeft");
            LabelName       ??= GetNodeOrNull<Label>("LabelName");

            if (LabelTop    != null) LabelTop.Text    = _cardInstance.GetValue(Direction.Top).ToString();
            if (LabelRight  != null) LabelRight.Text  = _cardInstance.GetValue(Direction.Right).ToString();
            if (LabelBottom != null) LabelBottom.Text = _cardInstance.GetValue(Direction.Bottom).ToString();
            if (LabelLeft   != null) LabelLeft.Text   = _cardInstance.GetValue(Direction.Left).ToString();
            if (LabelName   != null) LabelName.Text   = _cardInstance.Data.Name;

            SetOwnerColor(_cardInstance.OwnerId);
        }

        public void SetOwnerColor(int ownerId)
        {
            if (Background == null) return;
            var color = ownerId == 1 ? P1Color : P2Color;
            var style = new StyleBoxFlat();
            style.BgColor = color;
            style.SetCornerRadiusAll(4);
            Background.AddThemeStyleboxOverride("panel", style);
        }

        public void SetSelected(bool selected)
        {
            SelectionBorder ??= GetNodeOrNull<Panel>("SelectionBorder");
            if (SelectionBorder != null)
                SelectionBorder.Visible = selected;
        }

        public CardInstance GetCardInstance() => _cardInstance;
    }
}
