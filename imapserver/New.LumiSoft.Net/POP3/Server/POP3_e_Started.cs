using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.POP3.Server
{
    /// <summary>
    /// This class provides data for <b cref="POP3_Session.Started">POP3_Session.Started</b> event.
    /// </summary>
    public class POP3_e_Started : EventArgs
    {
        private string m_Response = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="response">POP3 server response.</param>
        internal POP3_e_Started(string response)
        {
            m_Response = response;
        }


        #region roperties implementation

        /// <summary>
        /// Gets or sets POP3 server response.
        /// </summary>
        /// <remarks>Response also MUST contain response code(+OK / -ERR). For example: "-ERR Session rejected."</remarks>
        public string Response
        {
            get{ return m_Response; }

            set{ m_Response = value; }
        }

        #endregion
    }
}
