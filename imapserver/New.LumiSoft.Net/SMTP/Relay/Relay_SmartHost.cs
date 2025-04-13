using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Relay
{
    /// <summary>
    /// This class holds smart host settings.
    /// </summary>
    public class Relay_SmartHost
    {
        private string  m_Host     = "";
        private int     m_Port     = 25;
        private SslMode m_SslMode  = SslMode.None;
        private string  m_UserName = null;
        private string  m_Password = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="host">Smart host name or IP address.</param>
        /// <param name="port">Smart host port.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>host</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public Relay_SmartHost(string host,int port) : this(host,port,SslMode.None,null,null)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="host">Smart host name or IP address.</param>
        /// <param name="port">Smart host port.</param>
        /// <param name="sslMode">Smart host SSL mode.</param>
        /// <param name="userName">Smart host user name.</param>
        /// <param name="password">Smart host password.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>host</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public Relay_SmartHost(string host,int port,SslMode sslMode,string userName,string password)
        {
            if(host == null){
                throw new ArgumentNullException("host");
            }
            if(host == ""){
                throw new ArgumentException("Argument 'host' value must be specified.");
            }
            if(port < 1){
                throw new ArgumentException("Argument 'port' value is invalid.");
            }

            m_Host     = host;
            m_Port     = port;
            m_SslMode  = sslMode;
            m_UserName = userName;
            m_Password = password;
        }


        #region override method Equals

        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>Returns true if two objects are equal.</returns>
        public override bool Equals(object obj)
        {
            if(obj == null){
                return false;
            }
            if(!(obj is Relay_SmartHost)){
                return false;
            }

            Relay_SmartHost smartHost = (Relay_SmartHost)obj;
            if(m_Host != smartHost.Host){
                return false;
            }
            else if(m_Port != smartHost.Port){
                return false;
            }
            else if(m_SslMode != smartHost.SslMode){
                return false;
            }
            else if(m_UserName != smartHost.UserName){
                return false;
            }
            else if(m_Password != smartHost.Password){
                return false;
            }

            return true;
        }

        #endregion

        #region override method GetHashCode

        /// <summary>
        /// Returns the hash code.
        /// </summary>
        /// <returns>Returns the hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets smart host name or IP address.
        /// </summary>
        public string Host
        {
            get{ return m_Host; }
        }

        /// <summary>
        /// Gets smart host port.
        /// </summary>
        public int Port
        {
            get{ return m_Port; }
        }

        /// <summary>
        /// Gets smart host SSL mode.
        /// </summary>
        public SslMode SslMode
        {
            get{ return m_SslMode; }
        }

        /// <summary>
        /// Gets smart host user name. Value null means no authentication used.
        /// </summary>
        public string UserName
        {
            get{ return m_UserName; }
        }

        /// <summary>
        /// Gets smart host password.
        /// </summary>
        public string Password
        {
            get{ return m_Password;}
        }

        #endregion

    }
}
