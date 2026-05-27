using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.UI
{
    public partial class HandNode : Control
    {
        [Export] public HBoxContainer CardSlots { get; set; }

        [Signal] public delegate void CardSelectedEventHandler(int handIndex, CardNode cardNode);

        private List<CardNode> _cardNodes = new();

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

                // Prevent HBoxContainer from stretching the card vertically
                cardNode.SizeFlagsVertical = SizeFlags.ShrinkCenter;

                CardSlots.AddChild(cardNode);
                cardNode.Initialize(hand[i]);
                cardNode.CustomMinimumSize = new Vector2(120, 160);
                cardNode.CallDeferred("set_size", new Vector2(120, 160));

                int index = i;
                var button = new Button();
                button.AnchorLeft   = 0;
                button.AnchorTop    = 0;
                button.AnchorRight  = 1;
                button.AnchorBottom = 1;
                button.OffsetLeft   = 0;
                button.OffsetTop    = 0;
                button.OffsetRight  = 0;
                button.OffsetBottom = 0;
                button.SelfModulate = new Color(1, 1, 1, 0);
                button.Pressed += () => OnCardSelected(index, cardNode);
                cardNode.AddChild(button);

                _cardNodes.Add(cardNode);
                GD.Print($"HandNode: added card {hand[i].Data.Name} at index {i}");
            }
        }

        public void RemoveCard(int handIndex)
        {
            if (handIndex < 0 || handIndex >= _cardNodes.Count) return;
            var cardNode = _cardNodes[handIndex];
            if (cardNode.GetParent() != null)
                cardNode.GetParent().RemoveChild(cardNode);
            _cardNodes.RemoveAt(handIndex);
            GD.Print($"HandNode: removed card at index {handIndex}");
        }

        public int GetCardNodeIndex(CardNode cardNode)
        {
            return _cardNodes.IndexOf(cardNode);
        }

        /// <summary>
        /// When interactive=false, removes the invisible click-button overlay from
        /// every card in the hand. Used for the AI hand display — cards are visible
        /// but the player cannot select them.
        /// </summary>
        public void SetInteractive(bool interactive)
        {
            if (interactive) return; // no-op: cards are interactive by default
            foreach (var cardNode in _cardNodes)
            {
                // The overlay button is the last child added in PopulateHand.
                // Find it by type and remove it.
                foreach (var child in cardNode.GetChildren())
                {
                    if (child is Button btn)
                    {
                        btn.QueueFree();
                        break;
                    }
                }
            }
        }

        private void OnCardSelected(int handIndex, CardNode cardNode)
        {
            GD.Print($"Hand card selected: index {handIndex}");
            EmitSignal(SignalName.CardSelected, handIndex, cardNode);
        }
    }
}