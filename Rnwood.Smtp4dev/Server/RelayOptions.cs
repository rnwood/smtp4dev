namespace Rnwood.Smtp4dev.Server
{
	public class RelayOptions
	{
		public bool IsEnabled => SmtpServer != string.Empty;

		public string SmtpServer { get; set; } = string.Empty;

		public int SmtpPort { get; set; } = 25;

		public string[] AllowedEmails { get; set; } = new string[0];

		public string SenderAddress { get; set; } = "";

		public string Login { get; set; } = "";

		public string Password { get; set; } = "";
	}
}