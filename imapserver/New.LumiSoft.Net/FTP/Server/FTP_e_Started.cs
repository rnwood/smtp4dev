using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.FTP.Server
{
    /// <summary>
    /// This class provides data for <b cref="FTP_Session.Started">FTP_Session.Started</b> event.
    /// </summary>
    public class FTP_e_Started : EventArgs
    {
        private string m_Response = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="response">POP3 server response.</param>
        internal FTP_e_Started(string response)
        {
            m_Response = response;
        }


        #region roperties implementation

        /// <summary>
        /// Gets or sets FTP server response.
        /// </summary>
        /// <remarks>Response also MUST contain response code(220 / 500). For example: "500 Session rejected."</remarks>
        public string Response
        {
            get{ return m_Response; }

            set{ m_Response = value; }
        }

        #endregion
    }
}
