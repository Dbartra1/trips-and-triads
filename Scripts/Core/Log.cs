using System;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Logging shim for production code.
	///
	/// PROBLEM
	/// -------
	/// Production rule code lives in Scripts/ and is exercised by xUnit tests
	/// in Tests/. When tests instantiate game objects, any direct call to
	/// Godot.GD.Print, Godot.GD.PrintErr, etc. crashes the test host with
	/// AccessViolationException, because the Godot.NativeInterop bindings
	/// dereference engine-allocated memory that is not initialised outside
	/// the Godot runtime.
	///
	/// SOLUTION
	/// --------
	/// All production logging goes through Log.Print / Log.PrintErr /
	/// Log.PushWarning. At runtime the shim probes once whether the Godot
	/// engine is available; if yes, calls are forwarded to GD; if no
	/// (i.e. running under xUnit), they go to Console.
	///
	/// USAGE
	/// -----
	///   Log.Print("Hand dealt.");
	///   Log.PrintErr("Save file unreadable.");
	///   Log.PushWarning("Sprite-sheet metadata invalid.");
	///
	/// MIGRATION
	/// ---------
	/// Replace every "GD.Print" / "GD.PrintErr" / "GD.PushWarning" call site
	/// in Scripts/Core/ and Scripts/Rules/ with the matching Log.* call.
	/// UI / scene / autoload code may keep direct GD calls — those are not
	/// reachable from the test runner.
	/// </summary>
	public static class Log
	{
		// Cached detection result. Computed once on first call.
		// True when Godot engine bindings are loadable and safe to call.
		private static bool? _godotAvailable;

		private static bool GodotAvailable
		{
			get
			{
				if (_godotAvailable.HasValue) return _godotAvailable.Value;

				// Try to touch a Godot type that requires the native runtime.
				// If the binding throws or fails to load, we're outside Godot.
				try
				{
					// Engine.GetVersionInfo touches native code; if outside Godot
					// it throws DllNotFoundException or AccessViolationException.
					_ = Godot.Engine.GetVersionInfo();
					_godotAvailable = true;
				}
				catch
				{
					_godotAvailable = false;
				}

				return _godotAvailable.Value;
			}
		}

		/// <summary>Normal log line.</summary>
		public static void Print(string message)
		{
			if (GodotAvailable) Godot.GD.Print(message);
			else                Console.WriteLine(message);
		}

		/// <summary>Error log line. Stderr in test mode; GD.PrintErr under Godot.</summary>
		public static void PrintErr(string message)
		{
			if (GodotAvailable) Godot.GD.PrintErr(message);
			else                Console.Error.WriteLine(message);
		}

		/// <summary>Non-fatal warning. Tagged as such in both environments.</summary>
		public static void PushWarning(string message)
		{
			if (GodotAvailable) Godot.GD.PushWarning(message);
			else                Console.Error.WriteLine("[warn] " + message);
		}
	}
}
