using System;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Logging shim for production code.
	///
	/// CONTEXT
	/// -------
	/// Production rule code in Scripts/ is exercised both by the Godot runtime
	/// (where logging is useful diagnostic noise in the editor's Output panel)
	/// and by the xUnit test suite in Tests/ (where logging from 369 simulated
	/// games produces hundreds of megabytes of output and crashes the test
	/// host's output collector).
	///
	/// Direct Godot.GD.* calls also crash the test host with
	/// AccessViolationException, because Godot.NativeInterop bindings
	/// dereference engine-allocated memory that is not initialised under
	/// xUnit. That exception is corrupted-state and cannot be caught.
	///
	/// SOLUTION
	/// --------
	/// - The shim uses only System.Console — no Godot bindings, so the test
	///   host never crashes on a log call.
	/// - By default, Log.Print is SILENT (early return). Routine play-by-play
	///   logging is opt-in.
	/// - Game code (GameSession, GameBoard, etc.) enables logging at startup
	///   by setting Log.Verbose = true, so editor / runtime players still get
	///   the diagnostic noise in the Output panel.
	/// - Tests leave Log.Verbose at its default false, so the suite produces
	///   a manageable amount of output.
	/// - Log.PrintErr and Log.PushWarning ALWAYS write — errors and warnings
	///   should never be silently dropped, in either context.
	///
	/// USAGE
	/// -----
	///   Log.Print("Hand dealt.");           // routine play-by-play, gated
	///   Log.PrintErr("Save unreadable.");   // always writes
	///   Log.PushWarning("Stale metadata."); // always writes
	///
	/// Game code that wants the editor logging back on should call
	/// Log.Verbose = true once at startup (e.g. in MainMenu._Ready or an
	/// autoload's _Ready).
	/// </summary>
	public static class Log
	{
		/// <summary>
		/// When false (default), Log.Print is a no-op. Game code should set
		/// this to true at startup. Tests leave it false so the suite stays
		/// quiet.
		/// </summary>
		public static bool Verbose { get; set; } = false;

		/// <summary>Routine play-by-play log line. Silent unless Verbose is set.</summary>
		public static void Print(string message)
		{
			if (!Verbose) return;
			Console.WriteLine(message);
		}

		/// <summary>Error log line. Always written.</summary>
		public static void PrintErr(string message)
		{
			Console.Error.WriteLine("[ERROR] " + message);
		}

		/// <summary>Non-fatal warning. Always written.</summary>
		public static void PushWarning(string message)
		{
			Console.Error.WriteLine("[WARN] " + message);
		}
	}
}