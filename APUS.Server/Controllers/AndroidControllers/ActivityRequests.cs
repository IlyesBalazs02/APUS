namespace APUS.Server.Controllers.AndroidControllers
{
	public sealed class NonGpsActivityUploadRequest
	{
		public string ActivityType { get; set; } = string.Empty;

		public long StartTimeUnixSeconds { get; set; }

		public int DurationSeconds { get; set; }
	}


}
