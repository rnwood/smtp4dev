using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Client
{
    /// <summary>
    /// This class represent IMAP command part.
    /// </summary>
    /// <remarks>
    /// Complete command consits of multiple parts.
    /// </remarks>
    internal class IMAP_Client_CmdPart
    {
        private IMAP_Client_CmdPart_Type m_Type  = IMAP_Client_CmdPart_Type.Constant;
        private string                   m_Value = null;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="type">Command part type.</param>
        /// <param name="data">Command data.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>data</b> is null reference.</exception>
        public IMAP_Client_CmdPart(IMAP_Client_CmdPart_Type type,string data)
        {
            if(data == null){
                throw new ArgumentNullException("data");
            }

            m_Type  = type;
            m_Value = data;
        }


        #region Properties implementation

        /// <summary>
        /// Gets command part ype.
        /// </summary>
        public IMAP_Client_CmdPart_Type Type
        {
            get{ return m_Type; }
        }

        /// <summary>
        /// Gets command part string value.
        /// </summary>
        public string Value
        {
            get{ return m_Value; }
        }

        #endregion
    }
}
