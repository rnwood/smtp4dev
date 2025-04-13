using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LumiSoft.Net.WebDav
{
    /// <summary>
    /// This class represents WebDav 'DAV:prop' element. Defined in RFC 4918 14.18.
    /// </summary>
    public class WebDav_Prop
    {
        private List<WebDav_p> m_pProperties = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public WebDav_Prop()
        {
            m_pProperties = new List<WebDav_p>();
        }


        #region static method Parse

        /// <summary>
        /// Parses WebDav_Prop from 'DAV:prop' element.
        /// </summary>
        /// <param name="propNode">The 'DAV:prop' element</param>
        /// <returns>Returns DAV prop.</returns>
        /// <exception cref="ArgumentNullException">Is raised when when <b>propNode</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when there are any parsing error.</exception>
        internal static WebDav_Prop Parse(XmlNode propNode)
        {
            if(propNode == null){
                throw new ArgumentNullException("propNode");
            }

            // Invalid response.
            if(!string.Equals(propNode.NamespaceURI + propNode.LocalName,"DAV:prop",StringComparison.InvariantCultureIgnoreCase)){
                throw new ParseException("Invalid DAV:prop value.");
            }

            WebDav_Prop retVal = new WebDav_Prop();

            foreach(XmlNode node in propNode.ChildNodes){
                // Resource type property.
                if(string.Equals(node.LocalName,"resourcetype",StringComparison.InvariantCultureIgnoreCase)){
                    retVal.m_pProperties.Add(WebDav_p_ResourceType.Parse(node));
                }
                // Default name-value property.
                else{
                    retVal.m_pProperties.Add(new WebDav_p_Default(node.NamespaceURI,node.LocalName,node.InnerXml));
                }
            }

            return retVal;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets properties.
        /// </summary>
        public WebDav_p[] Properties
        {
            get{ return m_pProperties.ToArray(); }
        }

        /// <summary>
        /// Gets WebDav 'DAV:resourcetype' property value. Returns null if no such property available.
        /// </summary>
        public WebDav_p_ResourceType Prop_ResourceType
        {
            get{
                foreach(WebDav_p property in m_pProperties){
                    if(property is WebDav_p_ResourceType){
                        return (WebDav_p_ResourceType)property;
                    }
                }

                return null;
            }
        }
        
        #endregion
    }
}
