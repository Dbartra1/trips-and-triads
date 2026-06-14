using Godot;

/// <summary>
/// Main menu — entry point for the game.
/// Continue loads an existing save; New Run wipes it and generates a fresh crew.
///
/// Animated background:
///   Plays res://Assets/Art/UI/MainMenuBackground.webm fullscreen via a
///   VideoStreamPlayer node created at runtime. If the file is absent, the
///   static PNG TextureRect is used as a fallback — no scene changes needed.
///
///   To replace with a higher-quality animation, drop a new WebM at the same
///   path. FFmpeg conversion from GIF:
///     ffmpeg -i input.gif -vf "scale=1920:1080,format=yuv420p" -c:v libvpx -b:v 2M -an MainMenuBackground.webm
/// </summary>
public partial class MainMenu : Control
{
	[Export] public Button NewRunButton    { get; set; }
	[Export] public Button ContinueButton  { get; set; }
	[Export] public Button QuitButton      { get; set; }

	private const string VideoPath = "res://Assets/Art/UI/MainMenuBackground.webm";

	public override void _Ready()
	{
		NewRunButton   ??= GetNodeOrNull<Button>("BottomArea/VBox/NewRunButton");
		ContinueButton ??= GetNodeOrNull<Button>("BottomArea/VBox/ContinueButton");
		QuitButton     ??= GetNodeOrNull<Button>("BottomArea/VBox/QuitButton");

		if (NewRunButton != null)
			NewRunButton.Pressed += OnNewRun;

		if (ContinueButton != null)
		{
			bool hasSave = SaveManager.SaveExists();
			ContinueButton.Visible = hasSave;
			ContinueButton.Pressed += OnContinue;
		}

		if (QuitButton != null)
			QuitButton.Pressed += () => GetTree().Quit();

		TryPlayAnimatedBackground();
	}

	/// <summary>
	/// Loads the WebM background video and plays it fullscreen behind the UI.
	/// Falls back silently to the static PNG TextureRect if the file is missing.
	/// </summary>
	private void TryPlayAnimatedBackground()
	{
		if (!ResourceLoader.Exists(VideoPath))
			return;

		var video = GD.Load<VideoStream>(VideoPath);
		if (video == null)
			return;

		// Hide the static fallback background.
		var staticBg = GetNodeOrNull<TextureRect>("Background");
		if (staticBg != null)
			staticBg.Visible = false;

		var player = new VideoStreamPlayer();
		player.Stream   = video;
		player.Autoplay = true;
		player.Loop     = true;

		// Stretch to fill the full screen — same preset used throughout the codebase.
		player.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		// Insert at index 0 so it sits behind the buttons.
		AddChild(player);
		MoveChild(player, 0);
		player.Play();
	}

	private void OnContinue()
	{
		GetTree().ChangeSceneToFile("res://Scenes/PreMatch/PreMatchScreen.tscn");
	}

	private void OnNewRun()
	{
		if (GameSession.Instance != null)
			GameSession.Instance.InitializeNewRun();

		GetTree().ChangeSceneToFile("res://Scenes/PreMatch/PreMatchScreen.tscn");
	}
}