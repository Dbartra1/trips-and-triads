using Godot;

/// <summary>
/// Main menu — entry point for the game.
/// Continue loads an existing save; New Run wipes it and generates a fresh crew.
///
/// Background:
///   The scene displays a static PNG (MainMenuBackground.png) by default.
///   If an OGV (Theora) video file exists at res://Assets/Art/UI/MainMenuBackground.ogv,
///   it is played fullscreen instead, replacing the static background. If the file
///   is absent, the static PNG remains visible — no scene changes needed when
///   the video arrives.
///
///   Why OGV and not WebM/MP4: Godot 4 only ships a Theora (OGV) loader natively.
///   WebM playback in Godot 4 requires a third-party GDExtension.
///
///   To swap in an animated background later, drop a Theora OGV file at the path
///   above. From an animated source via FFmpeg:
///     ffmpeg -i input.gif -vf "scale=1920:1080,format=yuv420p" \
///            -c:v libtheora -q:v 9 -an MainMenuBackground.ogv
///   (-q:v range is 0–10, higher = better quality / larger file. Try 9–10 to
///    minimize compression artifacts.)
///
///   Asset pipeline note for Procreate exports: see lore.md §14 for guidance on
///   exporting animations from Procreate to a format Godot can consume cleanly.
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
	/// Loads the OGV background video and plays it fullscreen behind the UI.
	/// Falls back silently to the static PNG TextureRect if the file is missing
	/// or cannot be loaded.
	/// </summary>
	private void TryPlayAnimatedBackground()
	{
		// FileAccess.FileExists checks the actual file on disk and works whether
		// or not the resource has been imported yet.
		if (!FileAccess.FileExists(VideoPath))
			return;

		VideoStream video;
		try
		{
			video = ResourceLoader.Load<VideoStream>(VideoPath);
		}
		catch (System.Exception ex)
		{
			GD.PushWarning($"[MainMenu] Failed to load background video: {ex.Message}");
			return;
		}

		if (video == null)
			return;

		// Hide the static fallback background only after we know the video loaded.
		var staticBg = GetNodeOrNull<TextureRect>("Background");
		if (staticBg != null)
			staticBg.Visible = false;

		var player = new VideoStreamPlayer();
		player.Name     = "AnimatedBackground";
		player.Stream   = video;
		player.Autoplay = true;
		player.Loop     = true;
		player.MouseFilter = Control.MouseFilterEnum.Ignore;

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