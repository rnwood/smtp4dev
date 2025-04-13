using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LumiSoft.Net.WebDav
{
    /// <summary>
    /// This class represents WebDav 'DAV:propstat' element. Defined in RFC 4918 14.22.
    /// </summary>
    public class WebDav_PropStat
    {
        private string      m_Status              = null;
        private string      m_ResponseDescription = null;
        private WebDav_Prop m_pProp               = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal WebDav_PropStat()
        {
        }


        #region static method Parse

        /// <summary>
        /// Parses WebDav_PropStat from 'DAV:propstat' element.
        /// </summary>
        /// <param name="propstatNode">The 'DAV:propstat' element</param>
        /// <returns>Returns DAV propstat.</returns>
        /// <exception cref="ArgumentNullException">Is raised when when <b>propstatNode</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when there are any parsing error.</exception>
        internal static WebDav_PropStat Parse(XmlNode propstatNode)
        {
            if(propstatNode == null){
                throw new ArgumentNullException("propstatNode");
            }

            // Invalid response.
            if(!string.Equals(propstatNode.NamespaceURI + propstatNode.LocalName,"DAV:propstat",StringComparison.InvariantCultureIgnoreCase)){
                throw new ParseException("Invalid DAV:propstat value.");
            }

            WebDav_PropStat retVAl = new WebDav_PropStat();

            foreach(XmlNode node in propstatNode.ChildNodes){
                if(string.Equals(node.LocalName,"status",StringComparison.InvariantCultureIgnoreCase)){
                    retVAl.m_Status = node.ChildNodes[0].Value;
                }
                else if(string.Equals(node.LocalName,"prop",StringComparison.InvariantCultureIgnoreCase)){
                    retVAl.m_pProp = WebDav_Prop.Parse(node);
                }                
            }

            return retVAl;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets property HTTP status.
        /// </summary>
        public string Status
        {
            get{ return m_Status; }
        }

        /// <summary>
        /// Gets human-readable status property description.
        /// </summary>
        public string ResponseDescription
        {
            get{ return m_ResponseDescription; }
        }
        
        /// <summary>
        /// Gets 'prop' element value.
        /// </summary>
        public WebDav_Prop Prop
        {
            get{ return m_pProp; }
        }
        
        #endregion
    }
}
