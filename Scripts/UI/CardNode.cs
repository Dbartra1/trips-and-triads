using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.UI
{
    public partial class CardNode : Control
    {
        [Export] public Panel Background { get; set; }
        [Export] public TextureRect CardArt { get; set; }
        [Export] public Label LabelTop { get; set; }
        [Export] public Label LabelRight { get; set; }
        [Export] public Label LabelBottom { get; set; }
        [Export] public Label LabelLeft { get; set; }

        private CardInstance _cardInstance;

        private static readonly Color P1Color = new Color("4a90d9");
        private static readonly Color P2Color = new Color("d94a4a");

        public void Initialize(CardInstance instance)
        {
            _cardInstance = instance;

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
            }

            if (LabelTop != null)
            {
                LabelTop.AnchorLeft   = 0.5f;
                LabelTop.AnchorRight  = 0.5f;
                LabelTop.AnchorTop    = 0f;
                LabelTop.AnchorBottom = 0f;
                LabelTop.OffsetLeft   = -10;
                LabelTop.OffsetRight  = 10;
                LabelTop.OffsetTop    = 4;
                LabelTop.OffsetBottom = 24;
                LabelTop.HorizontalAlignment = HorizontalAlignment.Center;
            }

            if (LabelBottom != null)
            {
                LabelBottom.AnchorLeft   = 0.5f;
                LabelBottom.AnchorRight  = 0.5f;
                LabelBottom.AnchorTop    = 1f;
                LabelBottom.AnchorBottom = 1f;
                LabelBottom.OffsetLeft   = -10;
                LabelBottom.OffsetRight  = 10;
                LabelBottom.OffsetTop    = -24;
                LabelBottom.OffsetBottom = -4;
                LabelBottom.HorizontalAlignment = HorizontalAlignment.Center;
            }

            if (LabelLeft != null)
            {
                LabelLeft.AnchorLeft   = 0f;
                LabelLeft.AnchorRight  = 0f;
                LabelLeft.AnchorTop    = 0.5f;
                LabelLeft.AnchorBottom = 0.5f;
                LabelLeft.OffsetLeft   = 4;
                LabelLeft.OffsetRight  = 24;
                LabelLeft.OffsetTop    = -10;
                LabelLeft.OffsetBottom = 10;
                LabelLeft.HorizontalAlignment = HorizontalAlignment.Center;
            }

            if (LabelRight != null)
            {
                LabelRight.AnchorLeft   = 1f;
                LabelRight.AnchorRight  = 1f;
                LabelRight.AnchorTop    = 0.5f;
                LabelRight.AnchorBottom = 0.5f;
                LabelRight.OffsetLeft   = -24;
                LabelRight.OffsetRight  = -4;
                LabelRight.OffsetTop    = -10;
                LabelRight.OffsetBottom = 10;
                LabelRight.HorizontalAlignment = HorizontalAlignment.Center;
            }

            Refresh();
        }

        public void Refresh()
        {
            if (_cardInstance == null) return;

            var data = _cardInstance.Data;

            if (LabelTop != null)    LabelTop.Text    = data.Top.ToString();
            if (LabelRight != null)  LabelRight.Text  = data.Right.ToString();
            if (LabelBottom != null) LabelBottom.Text = data.Bottom.ToString();
            if (LabelLeft != null)   LabelLeft.Text   = data.Left.ToString();

            SetOwnerColor(_cardInstance.OwnerId);
        }

        public void SetOwnerColor(int ownerId)
        {
            if (Background == null) return;
            Background.SelfModulate = ownerId == 1 ? P1Color : P2Color;
        }

        public CardInstance GetCardInstance() => _cardInstance;
    }
}