using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.TCP;
using LumiSoft.Net.AUTH;

namespace LumiSoft.Net.SMTP.Server
{
    /// <summary>
    /// This class implements SMTP server. Defined RFC 5321.
    /// </summary>
    public class SMTP_Server : TCP_Server<SMTP_Session>
    {
        private List<string> m_pServiceExtentions = null;
        private string       m_GreetingText       = "";
        private int          m_MaxBadCommands     = 30;
        private int          m_MaxTransactions    = 10;
        private int          m_MaxMessageSize     = 10000000;
        private int          m_MaxRecipients      = 100;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SMTP_Server()
        {
            m_pServiceExtentions = new List<string>();
            m_pServiceExtentions.Add(SMTP_ServiceExtensions.PIPELINING);
            m_pServiceExtentions.Add(SMTP_ServiceExtensions.SIZE);
            m_pServiceExtentions.Add(SMTP_ServiceExtensions.STARTTLS);
            m_pServiceExtentions.Add(SMTP_ServiceExtensions._8BITMIME);
            m_pServiceExtentions.Add(SMTP_ServiceExtensions.BINARYMIME);
            m_pServiceExtentions.Add(SMTP_ServiceExtensions.CHUNKING);
        }

        // TODO:

        //public override Dispose


        #region override method OnMaxConnectionsExceeded

        /// <summary>
        /// Is called when new incoming session and server maximum allowed connections exceeded.
        /// </summary>
        /// <param name="session">Incoming session.</param>
        /// <remarks>This method allows inhereted classes to report error message to connected client.
        /// Session will be disconnected after this method completes.
        /// </remarks>
        protected override void OnMaxConnectionsExceeded(SMTP_Session session)
        {
            session.TcpStream.WriteLine("421 Client host rejected: too many connections, please try again later.");
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
        protected override void OnMaxConnectionsPerIPExceeded(SMTP_Session session)
        {
            session.TcpStream.WriteLine("421 Client host rejected: too many connections from your IP(" + session.RemoteEndPoint.Address + "), please try again later.");
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets or sets SMTP server supported service extentions.
        /// Supported values: PIPELINING,SIZE,STARTTLS,8BITMIME,BINARYMIME,CHUNKING,DSN.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when null reference passed.</exception>
        public string[] ServiceExtentions
        {
            get{              
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
 
                return m_pServiceExtentions.ToArray(); 
            }

            set{
                if(value == null){
                    throw new ArgumentNullException("ServiceExtentions");
                }

                m_pServiceExtentions.Clear();

                foreach(string extention in value){
                    if(extention.ToUpper() == SMTP_ServiceExtensions.PIPELINING){
                        m_pServiceExtentions.Add(SMTP_ServiceExtensions.PIPELINING);
                    }
                    else if(extention.ToUpper() == SMTP_ServiceExtensions.SIZE){
                        m_pServiceExtentions.Add(SMTP_ServiceExtensions.SIZE);
                    }
                    else if(extention.ToUpper() == SMTP_ServiceExtensions.STARTTLS){
                        m_pServiceExtentions.Add(SMTP_ServiceExtensions.STARTTLS);
                    }
                    else if(extention.ToUpper() == SMTP_ServiceExtensions._8BITMIME){
                        m_pServiceExtentions.Add(SMTP_ServiceExtensions._8BITMIME);
                    }
                    else if(extention.ToUpper() == SMTP_ServiceExtensions.BINARYMIME){
                        m_pServiceExtentions.Add(SMTP_ServiceExtensions.BINARYMIME);
                    }
                    else if(extention.ToUpper() == SMTP_ServiceExtensions.CHUNKING){
                        m_pServiceExtentions.Add(SMTP_ServiceExtensions.CHUNKING);
                    }
                    else if(extention.ToUpper() == SMTP_ServiceExtensions.DSN){
                        m_pServiceExtentions.Add(SMTP_ServiceExtensions.DSN);
                    }
                }
            }
        }

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
        /// Gets or sets maximum mail transactions per session. Value 0 means unlimited.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int MaxTransactions
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_MaxTransactions; 
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 0){
                    throw new ArgumentException("Property 'MaxTransactions' value must be >= 0.");
                }

                m_MaxTransactions = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum message size in bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int MaxMessageSize
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_MaxMessageSize; 
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                if(value < 500){
                    throw new ArgumentException("Property 'MaxMessageSize' value must be >= 500.");
                }

                m_MaxMessageSize = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum allowed recipients per SMTP transaction.
        /// </summary>
        /// <remarks>According RFC 5321 this value SHOULD NOT be less than 100.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int MaxRecipients
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_MaxRecipients; 
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 1){
                    throw new ArgumentException("Property 'MaxRecipients' value must be >= 1.");
                }

                m_MaxRecipients = value;
            }
        }


        /// <summary>
        /// Gets SMTP service extentions list.
        /// </summary>
        internal List<string> Extentions
        {
            get{ return m_pServiceExtentions; }
        }

        #endregion

        #region Events implementation
                
        #endregion

    }
}
