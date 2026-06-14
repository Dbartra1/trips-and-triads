using Godot;

/// <summary>
/// Main menu — entry point for the game.
/// Continue loads an existing save; New Run wipes it and generates a fresh crew.
///
/// Background:
///   By default the scene displays the static PNG (MainMenuBackground.png)
///   referenced in MainMenu.tscn.
///
///   If an animated sprite sheet exists at:
///     res://Assets/Art/UI/MainMenuBackground_sheet.png
///   ...and a matching metadata file exists at:
///     res://Assets/Art/UI/MainMenuBackground_sheet.json
///   ...then the static PNG is replaced at runtime by a Sprite2D playing the
///   sheet on a loop.
///
///   The metadata JSON describes the sheet layout so we can change frame
///   counts or grid arrangements without touching code:
///   {
///     "columns": 6,
///     "rows": 4,
///     "frame_count": 24,
///     "fps": 24
///   }
///
///   If either file is missing or the metadata fails to parse, the static
///   PNG remains visible — no scene or code changes required to swap in the
///   animation later. See lore.md §14 for the artist export pipeline.
/// </summary>
public partial class MainMenu : Control
{
	[Export] public Button NewRunButton    { get; set; }
	[Export] public Button ContinueButton  { get; set; }
	[Export] public Button QuitButton      { get; set; }

	private const string SheetPath    = "res://Assets/Art/UI/MainMenuBackground_sheet.png";
	private const string MetadataPath = "res://Assets/Art/UI/MainMenuBackground_sheet.json";

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

	// ── Sprite-sheet animation ────────────────────────────────────────────

	private Sprite2D _animatedBg;
	private int      _frameCount;
	private int      _columns;
	private int      _rows;
	private double   _frameDuration;
	private double   _frameTimer;
	private int      _currentFrame;
	private bool     _animationActive;

	private void TryPlayAnimatedBackground()
	{
		if (!FileAccess.FileExists(SheetPath) || !FileAccess.FileExists(MetadataPath))
			return;

		// Load metadata
		var json = FileAccess.GetFileAsString(MetadataPath);
		var parser = new Json();
		if (parser.Parse(json) != Error.Ok)
		{
			GD.PushWarning($"[MainMenu] Failed to parse sprite-sheet metadata: {MetadataPath}");
			return;
		}

		var data = parser.Data.AsGodotDictionary();
		_columns       = data.ContainsKey("columns")     ? (int)data["columns"]     : 1;
		_rows          = data.ContainsKey("rows")        ? (int)data["rows"]        : 1;
		_frameCount    = data.ContainsKey("frame_count") ? (int)data["frame_count"] : (_columns * _rows);
		double fps     = data.ContainsKey("fps")         ? (double)data["fps"]      : 24.0;

		if (_frameCount <= 0 || _columns <= 0 || _rows <= 0 || fps <= 0.0)
		{
			GD.PushWarning("[MainMenu] Sprite-sheet metadata has invalid values.");
			return;
		}

		_frameDuration = 1.0 / fps;

		// Load the sheet texture
		Texture2D sheet;
		try
		{
			sheet = ResourceLoader.Load<Texture2D>(SheetPath);
		}
		catch (System.Exception ex)
		{
			GD.PushWarning($"[MainMenu] Failed to load sprite sheet: {ex.Message}");
			return;
		}
		if (sheet == null) return;

		int sheetWidth   = sheet.GetWidth();
		int sheetHeight  = sheet.GetHeight();
		int frameWidth   = sheetWidth  / _columns;
		int frameHeight  = sheetHeight / _rows;

		// Hide the static fallback background.
		var staticBg = GetNodeOrNull<TextureRect>("Background");
		if (staticBg != null)
			staticBg.Visible = false;

		// Build the Sprite2D with an AtlasTexture pointing at frame 0.
		_animatedBg = new Sprite2D
		{
			Name     = "AnimatedBackground",
			Centered = false,
			Texture  = MakeFrameAtlas(sheet, 0, frameWidth, frameHeight)
		};

		// Scale to fill the viewport.
		var viewport = GetViewportRect().Size;
		_animatedBg.Scale = new Vector2(
			viewport.X / frameWidth,
			viewport.Y / frameHeight
		);
		_animatedBg.Position = Vector2.Zero;

		AddChild(_animatedBg);
		MoveChild(_animatedBg, 0); // Behind the buttons.

		_currentFrame    = 0;
		_frameTimer      = 0.0;
		_animationActive = true;
	}

	public override void _Process(double delta)
	{
		if (!_animationActive || _animatedBg == null) return;

		_frameTimer += delta;
		if (_frameTimer < _frameDuration) return;

		_frameTimer -= _frameDuration;
		_currentFrame = (_currentFrame + 1) % _frameCount;

		// Build a fresh AtlasTexture per frame and reassign Sprite2D.Texture.
		// Mutating an existing AtlasTexture's Region in place does not reliably
		// trigger a redraw in Godot 4 — the Sprite2D needs the texture *property*
		// to change to invalidate its cached visual.
		if (_animatedBg.Texture is AtlasTexture current && current.Atlas != null)
		{
			int frameWidth  = (int)current.Region.Size.X;
			int frameHeight = (int)current.Region.Size.Y;
			_animatedBg.Texture = MakeFrameAtlas(
				current.Atlas, _currentFrame, frameWidth, frameHeight
			);
		}
	}

	private static AtlasTexture MakeFrameAtlas(Texture2D sheet, int frameIndex, int frameWidth, int frameHeight)
	{
		int columns = sheet.GetWidth() / frameWidth;
		int col = frameIndex % columns;
		int row = frameIndex / columns;
		return new AtlasTexture
		{
			Atlas  = sheet,
			Region = new Rect2(col * frameWidth, row * frameHeight, frameWidth, frameHeight)
		};
	}

	// ── Scene transitions ─────────────────────────────────────────────────

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