using Godot;
using System.Text;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;

namespace TripsAndTriads.UI
{
    public partial class CardContextMenu : PanelContainer
    {
        private VBoxContainer _vbox;
        private Label _flavorLabel;
        private Label _effectsLabel;
        private Label _modifiersLabel;

        public override void _Ready()
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            SizeFlagsVertical = SizeFlags.ShrinkBegin;

            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            style.BorderWidthLeft = 2;
            style.BorderWidthTop = 2;
            style.BorderWidthRight = 2;
            style.BorderWidthBottom = 2;
            style.BorderColor = new Color(0.24f, 0.8f, 0.93f);
            style.CornerRadiusTopLeft = 8;
            style.CornerRadiusTopRight = 8;
            style.CornerRadiusBottomRight = 8;
            style.CornerRadiusBottomLeft = 8;
            AddThemeStyleboxOverride("panel", style);

            _vbox = new VBoxContainer();
            _vbox.AddThemeConstantOverride("separation", 8);
            _vbox.AddThemeConstantOverride("margin_left", 12);
            _vbox.AddThemeConstantOverride("margin_right", 12);
            _vbox.AddThemeConstantOverride("margin_top", 12);
            _vbox.AddThemeConstantOverride("margin_bottom", 12);
            AddChild(_vbox);

            _flavorLabel = new Label();
            _flavorLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _flavorLabel.CustomMinimumSize = new Vector2(220, 0);
            _flavorLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            _flavorLabel.AddThemeFontSizeOverride("font_size", 14);
            _vbox.AddChild(_flavorLabel);

            var sep1 = new HSeparator();
            sep1.AddThemeColorOverride("color", new Color(0.2f, 0.2f, 0.3f));
            _vbox.AddChild(sep1);

            _effectsLabel = new Label();
            _effectsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _effectsLabel.CustomMinimumSize = new Vector2(220, 0);
            _effectsLabel.AddThemeColorOverride("font_color", new Color(0.24f, 0.8f, 0.93f));
            _effectsLabel.AddThemeFontSizeOverride("font_size", 13);
            _vbox.AddChild(_effectsLabel);

            var sep2 = new HSeparator();
            sep2.AddThemeColorOverride("color", new Color(0.2f, 0.2f, 0.3f));
            _vbox.AddChild(sep2);

            _modifiersLabel = new Label();
            _modifiersLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _modifiersLabel.CustomMinimumSize = new Vector2(220, 0);
            _modifiersLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.3f));
            _modifiersLabel.AddThemeFontSizeOverride("font_size", 13);
            _vbox.AddChild(_modifiersLabel);
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Right && !mouseEvent.Pressed)
            {
                Hide();
            }
        }

        public void Populate(CardInstance card, BoardState board, MatchConfig config, DistrictData district)
        {
            if (card == null || card.Data == null) return;

            var sbFlavor = new StringBuilder();
            var sbEffects = new StringBuilder();
            var sbModifiers = new StringBuilder();

            if (card.Data.Tier == Tier.Pro || card.Data.Tier == Tier.Hero)
            {
                sbFlavor.AppendLine($"[b][color=#f0c040]{card.Data.Name}[/color][/b]");
                sbFlavor.AppendLine($"[i]\"{GetFlavorText(card.Data)}\"[/i]");
                _flavorLabel.Visible = true;
            }
            else
            {
                _flavorLabel.Visible = false;
            }

            sbEffects.AppendLine("[b]Adjacency Effects:[/b]");
            bool hasEffects = false;
            if (board != null && card.Data.DomainType != DomainType.None)
            {
                string domainDesc = card.Data.DomainType switch
                {
                    DomainType.AegisProtocol => "Grants +1 to all edges of adjacent friendly cards.",
                    DomainType.Killzone => "Grants +2 to the two lowest edges of adjacent friendly cards.",
                    DomainType.LateralGrid => "Grants +2 to Left/Right edges of adjacent friendly cards.",
                    DomainType.Sprawl => "Grants +1 to edges of adjacent Commons faction cards.",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(domainDesc))
                {
                    sbEffects.AppendLine($"• {domainDesc}");
                    hasEffects = true;
                }
            }
            if (!hasEffects) sbEffects.AppendLine("• None");

            sbModifiers.AppendLine("[b]Active Modifiers:[/b]");
            bool hasModifiers = false;
            if (district != null)
            {
                if (!string.IsNullOrEmpty(district.Stake))
                {
                    sbModifiers.AppendLine($"• District Stake: {district.Stake}");
                    hasModifiers = true;
                }
                if (district.Protocols != null && district.Protocols.Count > 0)
                {
                    sbModifiers.AppendLine($"• Protocols: {string.Join(", ", district.Protocols)}");
                    hasModifiers = true;
                }
                if (!string.IsNullOrEmpty(district.Hazard))
                {
                    sbModifiers.AppendLine($"• Hazard: {district.Hazard}");
                    hasModifiers = true;
                }
                if (!string.IsNullOrEmpty(district.Description))
                {
                    sbModifiers.AppendLine($"• District: {district.Description}");
                    hasModifiers = true;
                }
            }
            if (!hasModifiers) sbModifiers.AppendLine("• None");

            _flavorLabel.Text = sbFlavor.ToString();
            _effectsLabel.Text = sbEffects.ToString();
            _modifiersLabel.Text = sbModifiers.ToString();

            CallDeferred(nameof(ResizeToContent));
        }

        private void ResizeToContent()
        {
            Vector2 desired = _vbox.GetCombinedMinimumSize();
            CustomMinimumSize = new Vector2(Mathf.Max(240, desired.X), Mathf.Min(desired.Y, 320));
            Size = CustomMinimumSize;
        }

        private string GetFlavorText(CardData data)
        {
            if (data.Tier == Tier.Hero)
                return "A legend in the making. The streets whisper their name with a mix of fear and reverence.";
            if (data.Tier == Tier.Pro)
                return "A seasoned operator. They've survived the lower tiers and know the cost of every move.";
            return "Just another face in the sprawl.";
        }
    }
}
