using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP NAMESPACE response. Defined in RFC 2342 5.
    /// </summary>
    public class IMAP_r_u_Namespace : IMAP_r_u
    {
        private IMAP_Namespace_Entry[] m_pPersonalNamespaces   = null;
        private IMAP_Namespace_Entry[] m_pOtherUsersNamespaces = null;
        private IMAP_Namespace_Entry[] m_pSharedNamespaces     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="personalNamespaces">Personal namespaces.</param>
        /// <param name="otherUsersNamespaces">Other users namespaces.</param>
        /// <param name="sharedNamespaces">Shared users namespaces.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>personalNamespaces</b> is null reference.</exception>
        public IMAP_r_u_Namespace(IMAP_Namespace_Entry[] personalNamespaces,IMAP_Namespace_Entry[] otherUsersNamespaces,IMAP_Namespace_Entry[] sharedNamespaces)
        {
            if(personalNamespaces == null){
                throw new ArgumentNullException("personalNamespaces");
            }

            m_pPersonalNamespaces   = personalNamespaces;
            m_pOtherUsersNamespaces = otherUsersNamespaces;
            m_pSharedNamespaces     = sharedNamespaces;
        }


        #region static method Parse

        /// <summary>
        /// Parses NAMESPACE response from namespace-response string.
        /// </summary>
        /// <param name="response">NAMESPACE response string.</param>
        /// <returns>Returns parsed NAMESPACE response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        public static IMAP_r_u_Namespace Parse(string response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            /* RFC 2342 5. NAMESPACE Command.
                Arguments: none

                Response:  an untagged NAMESPACE response that contains the prefix
                           and hierarchy delimiter to the server's Personal
                           Namespace(s), Other Users' Namespace(s), and Shared
                           Namespace(s) that the server wishes to expose. The
                           response will contain a NIL for any namespace class
                           that is not available. Namespace_Response_Extensions
                           MAY be included in the response.
                           Namespace_Response_Extensions which are not on the IETF
                           standards track, MUST be prefixed with an "X-".

                Result:    OK - Command completed
                           NO - Error: Can't complete command
                           BAD - argument invalid
                
                Example:
                    < A server that contains a Personal Namespace and a single Shared Namespace. >

                    C: A001 NAMESPACE
                    S: * NAMESPACE (("" "/")) NIL (("Public Folders/" "/"))
                    S: A001 OK NAMESPACE command completed
            */

            StringReader r = new StringReader(response);
            // Eat "*"
            r.ReadWord();
            // Eat "NAMESPACE"
            r.ReadWord();
            
            // Personal namespaces
            r.ReadToFirstChar();
            List<IMAP_Namespace_Entry> personal = new List<IMAP_Namespace_Entry>();
            if(r.SourceString.StartsWith("(")){
                StringReader rList = new StringReader(r.ReadParenthesized());
                while(rList.Available > 0){
                    string[] items = TextUtils.SplitQuotedString(rList.ReadParenthesized(),' ',true);
                    personal.Add(new IMAP_Namespace_Entry(items[0],items[1][0]));
                }
            }
            // NIL
            else{
                r.ReadWord();
            }

            // Other users namespaces
            r.ReadToFirstChar();
            List<IMAP_Namespace_Entry> other = new List<IMAP_Namespace_Entry>();
            if(r.SourceString.StartsWith("(")){
                StringReader rList = new StringReader(r.ReadParenthesized());
                while(rList.Available > 0){
                    string[] items = TextUtils.SplitQuotedString(rList.ReadParenthesized(),' ',true);
                    other.Add(new IMAP_Namespace_Entry(items[0],items[1][0]));
                }
            }
            // NIL
            else{
                r.ReadWord();
            }

            // Shared namespaces
            r.ReadToFirstChar();
            List<IMAP_Namespace_Entry> shared = new List<IMAP_Namespace_Entry>();
            if(r.SourceString.StartsWith("(")){
                StringReader rList = new StringReader(r.ReadParenthesized());
                while(rList.Available > 0){
                    string[] items = TextUtils.SplitQuotedString(rList.ReadParenthesized(),' ',true);
                    shared.Add(new IMAP_Namespace_Entry(items[0],items[1][0]));
                }
            }
            // NIL
            else{
                r.ReadWord();
            }

            return new IMAP_r_u_Namespace(personal.ToArray(),other.ToArray(),shared.ToArray());
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            // Example:    S: * NAMESPACE (("" "/")) NIL (("Public Folders/" "/"))

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* NAMESPACE ");
            if(m_pPersonalNamespaces != null && m_pPersonalNamespaces.Length > 0){
                retVal.Append("(");
                for(int i=0;i<m_pPersonalNamespaces.Length;i++){
                    if(i > 0){
                        retVal.Append(" ");
                    }
                    retVal.Append("(\"" + m_pPersonalNamespaces[i].NamespaceName + "\" \"" + m_pPersonalNamespaces[i].HierarchyDelimiter + "\")");
                }
                retVal.Append(")");
            }
            else{
                retVal.Append("NIL");
            }
            retVal.Append(" ");
            if(m_pOtherUsersNamespaces != null && m_pOtherUsersNamespaces.Length > 0){
                retVal.Append("(");
                for(int i=0;i<m_pOtherUsersNamespaces.Length;i++){
                    if(i > 0){
                        retVal.Append(" ");
                    }
                    retVal.Append("(\"" + m_pOtherUsersNamespaces[i].NamespaceName + "\" \"" + m_pOtherUsersNamespaces[i].HierarchyDelimiter + "\")");
                }
                retVal.Append(")");
            }
            else{
                retVal.Append("NIL");
            }
            retVal.Append(" ");
            if(m_pSharedNamespaces != null && m_pSharedNamespaces.Length > 0){
                retVal.Append("(");
                for(int i=0;i<m_pSharedNamespaces.Length;i++){
                    if(i > 0){
                        retVal.Append(" ");
                    }
                    retVal.Append("(\"" + m_pSharedNamespaces[i].NamespaceName + "\" \"" + m_pSharedNamespaces[i].HierarchyDelimiter + "\")");
                }
                retVal.Append(")");
            }
            else{
                retVal.Append("NIL");
            }
            retVal.Append("\r\n");
            

            return retVal.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets personal namespaces. Value null means not specified.
        /// </summary>
        public IMAP_Namespace_Entry[] PersonalNamespaces
        {
            get{ return m_pPersonalNamespaces; }
        }

        /// <summary>
        /// Gets other users namespaces. Value null means not specified.
        /// </summary>
        public IMAP_Namespace_Entry[] OtherUsersNamespaces
        {
            get{ return m_pOtherUsersNamespaces; }
        }

        /// <summary>
        /// Gets shared namespaces. Value null means not specified.
        /// </summary>
        public IMAP_Namespace_Entry[] SharedNamespaces
        {
            get{ return m_pSharedNamespaces; }
        }

        #endregion
    }
}
