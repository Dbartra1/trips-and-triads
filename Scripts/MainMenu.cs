using Godot;

/// <summary>
/// Main menu — entry point for the game.
/// Continue loads an existing save; New Run wipes it and generates a fresh crew.
/// </summary>
public partial class MainMenu : Control
{
	[Export] public Button NewRunButton      { get; set; }
	[Export] public Button ContinueButton   { get; set; }
	[Export] public Button QuitButton        { get; set; }

	public override void _Ready()
	{
		NewRunButton    ??= GetNodeOrNull<Button>("Center/VBox/NewRunButton");
		ContinueButton  ??= GetNodeOrNull<Button>("Center/VBox/ContinueButton");
		QuitButton      ??= GetNodeOrNull<Button>("Center/VBox/QuitButton");

		if (NewRunButton != null)
			NewRunButton.Pressed += OnNewRun;

		if (ContinueButton != null)
		{
			// Only show Continue when a save exists
			bool hasSave = SaveManager.SaveExists();
			ContinueButton.Visible = hasSave;
			ContinueButton.Pressed += OnContinue;
		}

		if (QuitButton != null)
			QuitButton.Pressed += () => GetTree().Quit();
	}

	private void OnContinue()
	{
		// The save was already loaded by GameSession._Ready (autoload fires before
		// MainMenu). Just go straight to PreMatchScreen with the loaded state.
		GetTree().ChangeSceneToFile("res://Scenes/PreMatch/PreMatchScreen.tscn");
	}

	private void OnNewRun()
	{
		if (GameSession.Instance != null)
			GameSession.Instance.InitializeNewRun();

		GetTree().ChangeSceneToFile("res://Scenes/PreMatch/PreMatchScreen.tscn");
	}
}