using Godot;

/// <summary>
/// Main menu — entry point for the game.
/// Continue loads an existing save; New Run wipes it and generates a fresh crew.
///
/// Animated background:
///   Plays res://Assets/Art/UI/MainMenuBackground.webm fullscreen via a
///   VideoStreamPlayer node created at runtime. If the file is absent or
///   fails to load, the static PNG TextureRect remains visible as a fallback.
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

	private const string VideoPath = "res://Assets/Art/UI/MainMenuBackground.ogv";

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
	/// Logs why it falls back so we can debug from the Godot console.
	/// </summary>
	private void TryPlayAnimatedBackground()
	{
		GD.Print($"[MainMenu] Attempting to load background video: {VideoPath}");

		// FileAccess.FileExists checks the literal file on disk and works even
		// when the resource has no .import sidecar yet — more reliable than
		// ResourceLoader.Exists() for raw WebM files.
		if (!FileAccess.FileExists(VideoPath))
		{
			GD.Print("[MainMenu] WebM not found on disk — using static fallback.");
			return;
		}

		VideoStream video;
		try
		{
			video = ResourceLoader.Load<VideoStream>(VideoPath);
		}
		catch (System.Exception ex)
		{
			GD.PushWarning($"[MainMenu] Failed to load WebM as VideoStream: {ex.Message}");
			return;
		}

		if (video == null)
		{
			GD.PushWarning("[MainMenu] ResourceLoader returned null for WebM — using static fallback.");
			return;
		}

		GD.Print("[MainMenu] WebM loaded — instantiating VideoStreamPlayer.");

		// Hide the static fallback background only after we know the video loaded.
		var staticBg = GetNodeOrNull<TextureRect>("Background");
		if (staticBg != null)
			staticBg.Visible = false;

		var player = new VideoStreamPlayer();
		player.Name     = "AnimatedBackground";
		player.Stream   = video;
		player.Autoplay = true;
		player.Loop     = true;

		// Fill the full screen.
		player.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		// Don't let the video swallow mouse input intended for the buttons.
		player.MouseFilter = Control.MouseFilterEnum.Ignore;

		// Insert at index 0 so it sits behind the buttons.
		AddChild(player);
		MoveChild(player, 0);
		player.Play();

		GD.Print($"[MainMenu] VideoStreamPlayer.Play() called. IsPlaying={player.IsPlaying()}");
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