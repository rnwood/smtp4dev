using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{    
    /// <summary>
    /// This base class for server SASL authentication mechanisms.
    /// </summary>
    public abstract class AUTH_SASL_ServerMechanism
    {
        private Dictionary<string,object> m_pTags = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AUTH_SASL_ServerMechanism()
        {
        }


        #region abstract method Reset

        /// <summary>
        /// Resets any authentication state data.
        /// </summary>
        public abstract void Reset();

        #endregion

        #region abstract method Continue

        /// <summary>
        /// Continues authentication process.
        /// </summary>
        /// <param name="clientResponse">Client sent SASL response.</param>
        /// <returns>Retunrns challange response what must be sent to client or null if authentication has completed.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>clientRespone</b> is null reference.</exception>
        public abstract byte[] Continue(byte[] clientResponse);

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if the authentication exchange has completed.
        /// </summary>
        public abstract bool IsCompleted
        {
            get;
        }

        /// <summary>
        /// Gets if user has authenticated sucessfully.
        /// </summary>
        public abstract bool IsAuthenticated
        {
            get;
        }

        /// <summary>
        /// Gets IANA-registered SASL authentication mechanism name.
        /// </summary>
        /// <remarks>The registered list is available from: http://www.iana.org/assignments/sasl-mechanisms .</remarks>
        public abstract string Name
        {
            get;
        }

        /// <summary>
        /// Gets if specified SASL mechanism is available only to SSL connection.
        /// </summary>
        public abstract bool RequireSSL
        {
            get;
        }

        /// <summary>
        /// Gets user login name.
        /// </summary>
        public abstract string UserName
        {
            get;
        }

        /// <summary>
        /// Gets user data items collection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public Dictionary<string,object> Tags
        {
            get{ return m_pTags; }
        }
        
        #endregion

    }
}
