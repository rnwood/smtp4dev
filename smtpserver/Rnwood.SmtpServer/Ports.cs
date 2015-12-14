namespace Rnwood.SmtpServer
{
    /// <summary>
    /// Enumeration of the different standard TCP ports that the server can listen on
    /// </summary>
    public enum Ports
    {
        /// <summary>
        /// Select a free port number automatically
        /// </summary>
        AssignAutomatically = 0,

        /// <summary>
        /// Use the standard IANA SMTP port - 25
        /// </summary>
        SMTP = 25,

        /// <summary>
        /// Use the standard IANA SMTP-over-SSL port - 465
        /// </summary>
        SMTPOverSSL = 465
    }
}