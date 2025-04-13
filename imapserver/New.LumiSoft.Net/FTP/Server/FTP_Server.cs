using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;

using LumiSoft.Net.TCP;
using LumiSoft.Net.AUTH;

namespace LumiSoft.Net.FTP.Server
{
	/// <summary>
	/// FTP Server component.
	/// </summary>
	public class FTP_Server : TCP_Server<FTP_Session>
	{
        private string    m_GreetingText     = "";
        private int       m_MaxBadCommands   = 30;
        private IPAddress m_pPassivePublicIP = null;
        private int       m_PassiveStartPort = 20000;

		/// <summary>
		/// Defalut constructor.
		/// </summary>
		public FTP_Server()
		{
            this.SessionIdleTimeout = 3600;
		}


        #region override method OnMaxConnectionsExceeded

        /// <summary>
        /// Is called when new incoming session and server maximum allowed connections exceeded.
        /// </summary>
        /// <param name="session">Incoming session.</param>
        /// <remarks>This method allows inhereted classes to report error message to connected client.
        /// Session will be disconnected after this method completes.
        /// </remarks>
        protected override void OnMaxConnectionsExceeded(FTP_Session session)
        {
            session.TcpStream.WriteLine("500 Client host rejected: too many connections, please try again later.");
        }

        #endregion

        #region override method OnMaxConnectionsPerIPExceeded

        /// <summary>
        /// Is called when new incoming session and server maximum allowed connections per connected IP exceeded.
        /// </summary>
        /// <param name="session">Incoming session.</param>
        /// <remarks>This method allows inhereted classes to report error message to connected client.
        /// Session will be disconnected after this method completes.
        /// </remarks>
        protected override void OnMaxConnectionsPerIPExceeded(FTP_Session session)
        {
            session.TcpStream.WriteLine("500 Client host rejected: too many connections from your IP(" + session.RemoteEndPoint.Address + "), please try again later.");
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets or sets server greeting text.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string GreetingText
        {
            get{                
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_GreetingText; }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                m_GreetingText = value;
            }
        }

        /// <summary>
        /// Gets or sets how many bad commands session can have before it's terminated. Value 0 means unlimited.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int MaxBadCommands
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_MaxBadCommands; 
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 0){
                    throw new ArgumentException("Property 'MaxBadCommands' value must be >= 0.");
                }

                m_MaxBadCommands = value;
            }
        }

        /// <summary>
        /// Gets or sets passive mode public IP address what is reported to clients. 
        /// This property is manly needed if FTP server is running behind NAT. 
        /// Value null means not spcified.
        /// </summary>
        public IPAddress PassivePublicIP
        {
            get{ return m_pPassivePublicIP; }

            set{ m_pPassivePublicIP = value; }
        }

        /// <summary>
        /// Gets or sets passive mode start port form which server starts using ports.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when ivalid value is passed.</exception>
        public int PassiveStartPort
        {
            get{ return m_PassiveStartPort; }

            set{
                if(value < 1){
                    throw new ArgumentException("Valu must be > 0 !");
                }

                m_PassiveStartPort = value;
            }
        }

        #endregion
	}
}
