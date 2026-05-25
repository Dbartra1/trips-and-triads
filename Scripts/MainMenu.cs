using Godot;

/// <summary>
/// Main menu — entry point for the game.
/// New Run initializes a fresh GameSession and goes to PreMatchScreen.
/// </summary>
public partial class MainMenu : Control
{
	[Export] public Button NewRunButton  { get; set; }
	[Export] public Button QuitButton    { get; set; }

	public override void _Ready()
	{
		NewRunButton ??= GetNodeOrNull<Button>("Center/VBox/NewRunButton");
		QuitButton   ??= GetNodeOrNull<Button>("Center/VBox/QuitButton");

		if (NewRunButton != null)
			NewRunButton.Pressed += OnNewRun;

		if (QuitButton != null)
			QuitButton.Pressed += () => GetTree().Quit();
	}

	private void OnNewRun()
	{
		// Initialize a fresh run in the session singleton
		if (GameSession.Instance != null)
			GameSession.Instance.InitializeNewRun();

		GetTree().ChangeSceneToFile("res://Scenes/PreMatch/PreMatchScreen.tscn");
	}
}
