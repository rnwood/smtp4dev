using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP ACL entry. Defined in RFC 4314 3.6.
    /// </summary>
    public class IMAP_Acl_Entry
    {
        private string m_Identifier = "";
        private string m_Rights     = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="identifier">ACL identifier. Normally this is user or group name.</param>
        /// <param name="rights">ACL rights string.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>identifier</b> or <b>rights</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_Acl_Entry(string identifier,string rights)
        {
            if(identifier == null){
                throw new ArgumentNullException("identifier");
            }
            if(identifier == string.Empty){
                throw new ArgumentException("Argument 'identifier' value must be specified.","identifier");
            }
            if(rights == null){
                throw new ArgumentNullException("rights");
            }

            m_Identifier = identifier;
            m_Rights     = rights;
        }


        #region Properties implementation

        /// <summary>
        /// Gets ACL identifier. Normally this is user or group name.
        /// </summary>
        public string Identifier
        {
            get{ return m_Identifier; }
        }

        /// <summary>
        /// Gets rights.
        /// </summary>
        public string Rights
        {
            get{ return m_Rights; }
        }

        #endregion
    }
}
