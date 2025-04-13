using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.MIME;

namespace LumiSoft.Net.Mail
{
    /// <summary>
    /// This class represents RFC 5322 3.4 Address class. 
    /// This class is base class for <see cref="Mail_t_Mailbox">mailbox address</see> and <see cref="Mail_t_Group">group address</see>.
    /// </summary>
    public abstract class Mail_t_Address
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Mail_t_Address()
        {
        }


        #region abstract method ToString

        /// <summary>
        /// Returns address as string value.
        /// </summary>
        /// <param name="wordEncoder">8-bit words ecnoder. Value null means that words are not encoded.</param>
        /// <returns>Returns address as string value.</returns>
        public abstract string ToString(MIME_Encoding_EncodedWord wordEncoder);

        #endregion

    }
}
