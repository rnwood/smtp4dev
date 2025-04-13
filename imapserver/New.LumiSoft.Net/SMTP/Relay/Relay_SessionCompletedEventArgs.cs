using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Relay
{
    /// <summary>
    /// This class provides data for <b>Relay_Server.SessionCompleted</b> event.
    /// </summary>
    public class Relay_SessionCompletedEventArgs
    {
        private Relay_Session m_pSession   = null;
        private Exception     m_pException = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="session">Relay session what completed processing.</param>
        /// <param name="exception">Exception what happened or null if relay completed successfully.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>session</b> is null.</exception>
        public Relay_SessionCompletedEventArgs(Relay_Session session,Exception exception)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }

            m_pSession   = session;
            m_pException = exception;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets relay session what completed processing.
        /// </summary>
        public Relay_Session Session
        {
            get{ return m_pSession; }
        }

        /// <summary>
        /// Gets Exception what happened or null if relay completed successfully.
        /// </summary>
        public Exception Exception
        {
            get{ return m_pException; }
        }

        #endregion

    }
}
