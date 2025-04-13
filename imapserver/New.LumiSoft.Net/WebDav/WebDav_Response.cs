using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LumiSoft.Net.WebDav
{
    /// <summary>
    /// This class represent WeDav 'DAV:response' element. Definded in RFC 4918 14.24.
    /// </summary>
    public class WebDav_Response
    {
        private string                m_HRef       = null;
        private List<WebDav_PropStat> m_pPropStats = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal WebDav_Response()
        {
            m_pPropStats = new List<WebDav_PropStat>();
        }


        #region static method Parse

        /// <summary>
        /// Parses WebDav_Response from 'DAV:response' element.
        /// </summary>
        /// <param name="reponseNode">The 'DAV:response' element</param>
        /// <returns>Returns DAV response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when when <b>responseNode</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when there are any parsing error.</exception>
        internal static WebDav_Response Parse(XmlNode reponseNode)
        {
            if(reponseNode == null){
                throw new ArgumentNullException("responseNode");
            }

            // Invalid response.
            if(!string.Equals(reponseNode.NamespaceURI + reponseNode.LocalName,"DAV:response",StringComparison.InvariantCultureIgnoreCase)){
                throw new ParseException("Invalid DAV:response value.");
            }

            WebDav_Response retVal = new WebDav_Response();

            foreach(XmlNode node in reponseNode.ChildNodes){
                if(string.Equals(node.LocalName,"href",StringComparison.InvariantCultureIgnoreCase)){
                    retVal.m_HRef = node.ChildNodes[0].Value;
                }
                else if(string.Equals(node.LocalName,"propstat",StringComparison.InvariantCultureIgnoreCase)){
                    retVal.m_pPropStats.Add(WebDav_PropStat.Parse(node));
                }
            }

            return retVal;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets response href.
        /// </summary>
        public string HRef
        {
            get{ return m_HRef; }
        }

        /// <summary>
        /// Gets 'propstat' elements.
        /// </summary>
        public WebDav_PropStat[] PropStats
        {
            get{ return m_pPropStats.ToArray(); }
        }

        #endregion
    }
}
