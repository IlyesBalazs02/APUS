using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace APUS.Server.Services.Implementations.FileServices
{
	public class HuberRegressor : IHuberRegressor
	{
		private readonly IWebHostEnvironment _env;
		private readonly ILogger<HuberRegressor> _logger;

		private const string ScriptFileName = "HuberRegressor.py";

		public HuberRegressor(
			IWebHostEnvironment env,
			ILogger<HuberRegressor> logger)
		{
			_env = env;
			_logger = logger;
		}

		public async Task TrainAsync(string userId, string filePath)
		{
			ValidateParams(userId, filePath);

			var workingDir = GetUserModelDir(userId);
			var scriptPath = GetScriptPath();

			var json = await RunPythonAsync(
				workingDir,
				scriptPath,
				args: $"train \"{filePath}\""
			);

		}

		public async Task<double?> PredictTotalTimeSecondsAsync(string userId, string filePath)
		{
			ValidateParams(userId, filePath);

			var workingDir = GetUserModelDir(userId);
			var scriptPath = GetScriptPath();

			var json = await RunPythonAsync(
				workingDir,
				scriptPath,
				args: $"predict \"{filePath}\""
			);

			_logger.LogInformation("Prediction raw JSON: {Json}", json);


			try
			{
				var doc = JsonSerializer.Deserialize<PredictResult>(json);
				return doc?.PredictedSeconds;
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to parse prediction JSON: {Json}", json);
				return null;
			}
		}

		public async Task<(double lat, double lon, double progress)?>
			CoordinateAtSecondsAsync(string userId, string filePath, double seconds)
		{
			ValidateParams(userId, filePath);

			var workingDir = GetUserModelDir(userId);
			var scriptPath = GetScriptPath();

			var json = await RunPythonAsync(
				workingDir,
				scriptPath,
				args: $"where \"{filePath}\" {seconds.ToString(CultureInfo.InvariantCulture)}"
			);

			try
			{
				var doc = JsonSerializer.Deserialize<WhereResult>(json);
				if (doc == null) return null;
				return (doc.Lat, doc.Lon, doc.Progress);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to parse where JSON: {Json}", json);
				return null;
			}
		}

		//  HELPERS

		private void ValidateParams(string userId, string filePath)
		{
			if (string.IsNullOrWhiteSpace(userId))
				throw new ArgumentNullException(nameof(userId));
			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentNullException(nameof(filePath));
		}

		private string GetUserModelDir(string userId)
		{
			var modelDir = Path.Combine(_env.WebRootPath, "Users", userId, "LAModels");
			Directory.CreateDirectory(modelDir);
			return modelDir;
		}

		private string GetScriptPath()
		{
			var scriptPath = Path.Combine(
				_env.ContentRootPath,
				"Services",
				"Implementations",
				"FileServices",
				ScriptFileName);

			if (!File.Exists(scriptPath))
			{
				throw new FileNotFoundException($"LinearAgression.py not found at: {scriptPath}");
			}

			return scriptPath;
		}

		private async Task<string> RunPythonAsync(string workingDir, string scriptPath, string args)
		{
			if (string.IsNullOrWhiteSpace(workingDir))
				throw new ArgumentException("workingDir is required", nameof(workingDir));

			if (string.IsNullOrWhiteSpace(scriptPath))
				throw new ArgumentException("scriptPath is required", nameof(scriptPath));

			if (!File.Exists(scriptPath))
				throw new FileNotFoundException("Python script not found", scriptPath);

			// Prefer a stable, non-versioned executable:
			// - "python" works for python.org installs and usually for Store Python (shim).
			// - "py" works if Python Launcher is installed.
			// If you want, wire this to IConfiguration (e.g. _config["Python:Executable"]).
			var candidates = new[]
			{
		"python",
		"py"
	};

			string? pythonExe = null;

			foreach (var c in candidates)
			{
				if (LooksLikePath(c))
				{
					if (File.Exists(c))
					{
						pythonExe = c;
						break;
					}
				}
				else
				{
					// Name-based resolution (PATH/App Execution Aliases)
					if (await CanStartProcessAsync(c, "--version", workingDir))
					{
						pythonExe = c;
						break;
					}
				}
			}

			if (pythonExe is null)
				throw new InvalidOperationException(
					"No usable Python executable found. Install Python (python.org recommended) " +
					"or ensure 'python' or 'py' is available on PATH/App Execution Aliases.");

			// If using "py", you may want "-3" to force Python 3.
			var fullArgs = pythonExe.Equals("py", StringComparison.OrdinalIgnoreCase)
				? $"-3 \"{scriptPath}\" {args}"
				: $"\"{scriptPath}\" {args}";

			var psi = new ProcessStartInfo
			{
				FileName = pythonExe,
				Arguments = fullArgs,
				WorkingDirectory = workingDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var proc = new Process { StartInfo = psi };

			_logger.LogInformation("Running Python: {FileName} {Arguments} (WD={WorkingDir})",
				psi.FileName, psi.Arguments, workingDir);

			try
			{
				proc.Start();
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(
					$"Failed to start Python process. FileName='{psi.FileName}', WD='{workingDir}'. " +
					"Ensure Python is installed and accessible.", ex);
			}

			var stdoutTask = proc.StandardOutput.ReadToEndAsync();
			var stderrTask = proc.StandardError.ReadToEndAsync();

			await proc.WaitForExitAsync();

			var stdout = await stdoutTask;
			var stderr = await stderrTask;

			if (!string.IsNullOrWhiteSpace(stderr))
				_logger.LogWarning("Python stderr: {Stderr}", stderr);

			if (proc.ExitCode != 0)
			{
				_logger.LogError(
					"Python script exited with code {Code}. Args: {Args}. Stdout: {Stdout} Stderr: {Stderr}",
					proc.ExitCode, fullArgs, stdout, stderr);

				throw new InvalidOperationException(
					$"Python script failed (exit={proc.ExitCode}). Stderr: {TrimForException(stderr)}");
			}

			var lines = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var jsonLine = lines.Length > 0 ? lines[^1].Trim() : string.Empty;

			if (string.IsNullOrWhiteSpace(jsonLine))
				throw new InvalidOperationException($"Python returned no output. Stdout: {TrimForException(stdout)}");

			_logger.LogInformation("Python stdout (last line as JSON): {JsonLine}", jsonLine);

			return jsonLine;

			static bool LooksLikePath(string s) => s.Contains('\\') || s.Contains('/') || s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

			static string TrimForException(string s)
				=> s.Length <= 2000 ? s : s.Substring(0, 2000) + "...";

			static async Task<bool> CanStartProcessAsync(string fileName, string arguments, string workingDir)
			{
				try
				{
					var psi = new ProcessStartInfo
					{
						FileName = fileName,
						Arguments = arguments,
						WorkingDirectory = workingDir,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true
					};

					using var p = new Process { StartInfo = psi };
					p.Start();

					var t1 = p.StandardOutput.ReadToEndAsync();
					var t2 = p.StandardError.ReadToEndAsync();

					await p.WaitForExitAsync();
					await Task.WhenAll(t1, t2);

					return p.ExitCode == 0;
				}
				catch
				{
					return false;
				}
			}
		}


		private sealed class PredictResult
		{
			[JsonPropertyName("predicted_seconds")]
			public double PredictedSeconds { get; set; }
		}

		private sealed class WhereResult
		{
			[JsonPropertyName("seconds")]
			public double Seconds { get; set; }

			[JsonPropertyName("lat")]
			public double Lat { get; set; }

			[JsonPropertyName("lon")]
			public double Lon { get; set; }

			[JsonPropertyName("progress")]
			public double Progress { get; set; }
		}
	}
}
