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
        private bool     _isOccupied = false;

        [Signal] public delegate void CellClickedEventHandler(int row, int col);

        // ── Setup ─────────────────────────────────────────────────────────────

        public void Initialize(int row, int col)
        {
            Row = row;
            Col = col;

            // CellNode itself must stop mouse events so _CanDropData fires here.
            MouseFilter = MouseFilterEnum.Stop;

            // Hover highlight — signals, not virtual overrides (Godot 4 C# binding)
            MouseEntered += OnMouseEntered;
            MouseExited  += OnMouseExited;

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
                // Pass mouse events through to CellNode so drop target works.
                // Hover highlight is handled by CellNode._GuiInput below.
                CellButton.MouseFilter  = MouseFilterEnum.Ignore;
                GD.Print($"CellButton set to Ignore at ({Row},{Col}) — drops handled by CellNode.");
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

        // ── Drag-and-drop target ──────────────────────────────────────────────

        /// <summary>Accept a drop only if the cell is empty and the data is a hand index.</summary>
        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            return !_isOccupied && data.VariantType == Variant.Type.Int;
        }

        /// <summary>Drop: emit CellClicked with this cell's coordinates.
        /// GameBoard's OnCellClicked handles the placement using the hand index
        /// that was stored when the drag started (via the DragStarted signal).</summary>
        public override void _DropData(Vector2 atPosition, Variant data)
        {
            if (_isOccupied) return;
            GD.Print($"Card dropped at ({Row},{Col})");
            EmitSignal(SignalName.CellClicked, Row, Col);
        }

        // ── Hover effect via GuiInput (replaces CellButton's built-in hover) ──

        public override void _GuiInput(InputEvent ev)
        {
            if (_isOccupied) return;
            if (ev is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
            {
                // Fallback: direct click on cell (no card was dragged — e.g. editor testing)
                EmitSignal(SignalName.CellClicked, Row, Col);
            }
        }

        private void OnMouseEntered()
        {
            if (!_isOccupied)
                Modulate = new Color(0.8f, 0.9f, 1.0f, 1.0f);
        }

        private void OnMouseExited()
        {
            Modulate = Colors.White;
        }

        // ── Card placement ────────────────────────────────────────────────────

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

            // Board cards are not draggable
            cardNode.SetDraggable(false);
        }

        /// <summary>
        /// Refresh card display stats (non-capture update — no flip animation).
        /// </summary>
        public void RefreshCard()
        {
            _currentCard?.Refresh();
        }

        /// <summary>
        /// Play the flip animation on the placed card (called when captured).
        /// Reads the new OwnerId from the CardInstance, which GameManager has already updated.
        /// </summary>
        public void FlipCard()
        {
            if (_currentCard == null) return;
            int newOwnerId = _currentCard.GetCardInstance()?.OwnerId ?? 1;
            _currentCard.FlipToOwner(newOwnerId);
        }

        public bool IsOccupied() => _isOccupied;
    }
}