using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.TCP;
using LumiSoft.Net.AUTH;

namespace LumiSoft.Net.POP3.Server
{
    /// <summary>
    /// This class implements POP3 server. Defined RFC 1939.
    /// </summary>
    public class POP3_Server : TCP_Server<POP3_Session>
    {        
        private string m_GreetingText   = "";
        private int    m_MaxBadCommands = 30;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public POP3_Server()
        {
        }


        #region override method OnMaxConnectionsExceeded

        /// <summary>
        /// Is called when new incoming session and server maximum allowed connections exceeded.
        /// </summary>
        /// <param name="session">Incoming session.</param>
        /// <remarks>This method allows inhereted classes to report error message to connected client.
        /// Session will be disconnected after this method completes.
        /// </remarks>
        protected override void OnMaxConnectionsExceeded(POP3_Session session)
        {
            session.TcpStream.WriteLine("-ERR Client host rejected: too many connections, please try again later.");
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
        protected override void OnMaxConnectionsPerIPExceeded(POP3_Session session)
        {
            session.TcpStream.WriteLine("-ERR Client host rejected: too many connections from your IP(" + session.RemoteEndPoint.Address + "), please try again later.");
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

        #endregion
    }
}
