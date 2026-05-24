using Godot;

namespace TripsAndTriads.UI
{
    public partial class CellNode : Control
    {
        [Export] public Button  CellButton    { get; set; }
        [Export] public Control CardContainer { get; set; }

        public int Row { get; private set; }
        public int Col { get; private set; }

        private const int CardWidth  = 120;
        private const int CardHeight = 160;
        private const int CardOffset = 16;

        private CardNode _currentCard;
        private bool _isOccupied = false;

        [Signal] public delegate void CellClickedEventHandler(int row, int col);

        public void Initialize(int row, int col)
        {
            Row = row;
            Col = col;

            if (CellButton != null)
            {
                CellButton.AnchorLeft   = 0;
                CellButton.AnchorTop    = 0;
                CellButton.AnchorRight  = 1;
                CellButton.AnchorBottom = 1;
                CellButton.OffsetLeft   = 0;
                CellButton.OffsetTop    = 0;
                CellButton.OffsetRight  = 0;
                CellButton.OffsetBottom = 0;
                CellButton.MouseFilter  = MouseFilterEnum.Stop;
                CellButton.Pressed      += OnButtonPressed;
                CellButton.MouseEntered += OnMouseEntered;
                CellButton.MouseExited  += OnMouseExited;
                GD.Print($"CellButton wired at ({Row},{Col}) size={CellButton.Size}");
            }
            else
            {
                GD.PrintErr($"CellButton is NULL at ({Row},{Col})");
            }

            if (CardContainer != null)
            {
                CardContainer.AnchorLeft   = 0;
                CardContainer.AnchorTop    = 0;
                CardContainer.AnchorRight  = 1;
                CardContainer.AnchorBottom = 1;
                CardContainer.OffsetLeft   = 0;
                CardContainer.OffsetTop    = 0;
                CardContainer.OffsetRight  = 0;
                CardContainer.OffsetBottom = 0;
                CardContainer.MouseFilter  = MouseFilterEnum.Ignore;
            }
        }

        private void OnButtonPressed()
        {
            GD.Print($"Button pressed at ({Row},{Col})");
            if (!_isOccupied)
                EmitSignal(SignalName.CellClicked, Row, Col);
        }

        private void OnMouseEntered()
        {
            if (!_isOccupied && CellButton != null)
                CellButton.Modulate = new Color("3a3a5a");
        }

        private void OnMouseExited()
        {
            if (!_isOccupied && CellButton != null)
                CellButton.Modulate = new Color("ffffff");
        }

        public void PlaceCard(CardNode cardNode)
        {
            _currentCard = cardNode;
            _isOccupied  = true;

            if (CellButton != null)
                CellButton.Disabled = true;

            cardNode.SizeFlagsVertical   = SizeFlags.ShrinkCenter;
            cardNode.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;

            CardContainer.AddChild(cardNode);
            cardNode.Position = new Vector2(CardOffset, CardOffset);

            cardNode.CustomMinimumSize = new Vector2(CardWidth, CardHeight);
            cardNode.CallDeferred("set_size", new Vector2(CardWidth, CardHeight));
        }

        public void RefreshCard()
        {
            _currentCard?.Refresh();
        }

        public bool IsOccupied() => _isOccupied;
    }
}