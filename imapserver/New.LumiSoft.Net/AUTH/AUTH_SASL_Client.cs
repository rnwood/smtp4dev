using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// This base class for client SASL authentication mechanisms. Defined in RFC 4422.
    /// </summary>
    public abstract class AUTH_SASL_Client
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public AUTH_SASL_Client()
        {
        }


        #region abstract method Continue

        /// <summary>
        /// Continues authentication process.
        /// </summary>
        /// <param name="serverResponse">Server sent SASL response.</param>
        /// <returns>Returns challange request what must be sent to server or null if authentication has completed.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>serverResponse</b> is null reference.</exception>
        public abstract byte[] Continue(byte[] serverResponse);

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
        /// Gets IANA-registered SASL authentication mechanism name.
        /// </summary>
        /// <remarks>The registered list is available from: http://www.iana.org/assignments/sasl-mechanisms .</remarks>
        public abstract string Name
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
        /// Gets if the authentication method supports SASL client "inital response".
        /// </summary>
        public virtual bool SupportsInitialResponse
        {
            get{ return false; }
        }

        #endregion
    }
}
