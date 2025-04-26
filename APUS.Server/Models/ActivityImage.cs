namespace APUS.Server.Models
{
	public class ActivityImage
	{
		public Guid Id { get; set; }
		public string FilePath { get; set; }
		public string FileName { get; set; }
		public string ContentType { get; set; }

		public ActivityImage(string Filepath, string Filename, string Contentype)
		{
			Id = Guid.NewGuid();
			FileName = Filename;
			FilePath = Filepath;
			ContentType = Contentype;
		}
	}
}
