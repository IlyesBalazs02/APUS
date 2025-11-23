using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace APUS.Server.Services.Implementations.FileServices
{
	public class LinearAggression : ILinearAggression
	{
		private readonly IWebHostEnvironment _env;
		private readonly ILogger<LinearAggression> _logger;

		private const string ScriptFileName = "LinearAgression.py";

		public LinearAggression(
			IWebHostEnvironment env,
			ILogger<LinearAggression> logger)
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
			var psi = new ProcessStartInfo
			{
				FileName = "py",
				Arguments = $"\"{scriptPath}\" {args}",
				WorkingDirectory = workingDir,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var proc = new Process { StartInfo = psi };

			_logger.LogInformation("Running Python: {FileName} {Arguments} (WD={WorkingDir})",
				psi.FileName, psi.Arguments, workingDir);

			proc.Start();

			var stdoutTask = proc.StandardOutput.ReadToEndAsync();
			var stderrTask = proc.StandardError.ReadToEndAsync();

			await proc.WaitForExitAsync();

			var stdout = await stdoutTask;
			var stderr = await stderrTask;

			if (!string.IsNullOrWhiteSpace(stderr))
			{
				_logger.LogWarning("Python stderr: {Stderr}", stderr);
			}

			if (proc.ExitCode != 0)
			{
				_logger.LogError(
					"Python script exited with code {Code}. Args: {Args}. Stdout: {Stdout} Stderr: {Stderr}",
					proc.ExitCode, args, stdout, stderr);
				throw new InvalidOperationException($"Python script failed with exit code {proc.ExitCode}");
			}

			return stdout;
		}

		// JSON DTOs

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
