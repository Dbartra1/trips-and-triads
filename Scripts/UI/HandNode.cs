using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.UI
{
    public partial class HandNode : Control
    {
        [Export] public VBoxContainer CardSlots { get; set; }

        [Signal] public delegate void CardSelectedEventHandler(int handIndex, CardNode cardNode);

        private List<CardNode> _cardNodes = new();

        // ── Population ────────────────────────────────────────────────────────

        /// <summary>
        /// Populate the hand display. Each card gets drag support enabled (for the
        /// player's hand) or disabled (for the AI hand) via SetInteractive.
        /// </summary>
        public void PopulateHand(List<CardInstance> hand)
        {
            if (CardSlots == null)
            {
                GD.PrintErr("HandNode: CardSlots is null");
                return;
            }

            var cardScene = GD.Load<PackedScene>("res://Scenes/Card/CardNode.tscn");
            if (cardScene == null)
            {
                GD.PrintErr("HandNode: could not load CardNode.tscn");
                return;
            }

            foreach (var child in CardSlots.GetChildren())
                child.QueueFree();
            _cardNodes.Clear();

            GD.Print($"HandNode: populating {hand.Count} cards");

            for (int i = 0; i < hand.Count; i++)
            {
                var cardNode = cardScene.Instantiate<CardNode>();

                cardNode.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                CardSlots.AddChild(cardNode);
                cardNode.Initialize(hand[i]);
                cardNode.CustomMinimumSize = new Vector2(120, 160);
                cardNode.CallDeferred("set_size", new Vector2(120, 160));

                // Enable dragging on this card — hand index is the drag data
                cardNode.SetDraggable(true, i);
                // DragStarted → CardSelected so GameBoard can track the active card
                int capturedIndex = i;
                cardNode.DragStarted += (node) =>
                    EmitSignal(SignalName.CardSelected, capturedIndex, node);

                _cardNodes.Add(cardNode);
                GD.Print($"HandNode: added card {hand[i].Data.Name} at index {i}");
            }
        }

        // ── Removal ───────────────────────────────────────────────────────────

        public void RemoveCard(int handIndex)
        {
            if (handIndex < 0 || handIndex >= _cardNodes.Count) return;
            var cardNode = _cardNodes[handIndex];
            if (cardNode.GetParent() != null)
                cardNode.GetParent().RemoveChild(cardNode);
            _cardNodes.RemoveAt(handIndex);
            GD.Print($"HandNode: removed card at index {handIndex}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public int GetCardNodeIndex(CardNode cardNode)
        {
            return _cardNodes.IndexOf(cardNode);
        }

        /// <summary>
        /// Returns the global position of the card node at handIndex.
        /// Used by GameBoard to calculate AI card animation start position.
        /// </summary>
        public Vector2 GetCardGlobalPosition(int handIndex)
        {
            if (handIndex < 0 || handIndex >= _cardNodes.Count)
                return GlobalPosition;
            return _cardNodes[handIndex].GlobalPosition;
        }

        public int Count => _cardNodes.Count;

        /// <summary>
        /// Disable drag/interaction (AI hand is display-only).
        /// </summary>
        public void SetInteractive(bool interactive)
        {
            for (int i = 0; i < _cardNodes.Count; i++)
                _cardNodes[i].SetDraggable(interactive, i);
        }
    }
}
