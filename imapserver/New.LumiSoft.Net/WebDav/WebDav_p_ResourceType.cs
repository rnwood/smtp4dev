using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LumiSoft.Net.WebDav
{
    /// <summary>
    /// This class represents WebDav 'DAV:resourcetype' property. Defined in RFC 4918 15.9.
    /// </summary>
    public class WebDav_p_ResourceType : WebDav_p
    {
        private List<string> m_pItems = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public WebDav_p_ResourceType()
        {
            m_pItems = new List<string>();
        }


        #region mehtod Contains

        /// <summary>
        /// Checks if this 'resourcetype' property contains the specified resource type.
        /// </summary>
        /// <param name="resourceType">Resource type to check.</param>
        /// <returns>Retruns true if the colletion contains specified resource type.</returns>
        public bool Contains(string resourceType)
        {
            foreach(string item in m_pItems){
                if(string.Equals(resourceType,item,StringComparison.InvariantCultureIgnoreCase)){
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region static method Parse

        /// <summary>
        /// Parses WebDav_p_ResourceType from 'DAV:resourcetype' xml element.
        /// </summary>
        /// <param name="resourcetypeNode">The 'DAV:resourcetype' xml element.</param>
        /// <returns>Returns DAV resourcetype.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>resourcetypeNode</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when there are any parsing error.</exception>
        internal static WebDav_p_ResourceType Parse(XmlNode resourcetypeNode)
        {
            if(resourcetypeNode == null){
                throw new ArgumentNullException("resourcetypeNode");
            }

            // Invalid response.
            if(!string.Equals(resourcetypeNode.NamespaceURI + resourcetypeNode.LocalName,"DAV:resourcetype",StringComparison.InvariantCultureIgnoreCase)){
                throw new ParseException("Invalid DAV:resourcetype value.");
            }

            WebDav_p_ResourceType retVal = new WebDav_p_ResourceType();

            foreach(XmlNode node in resourcetypeNode.ChildNodes){
                retVal.m_pItems.Add(node.NamespaceURI + node.LocalName);
            }

            return retVal;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets property namespace.
        /// </summary>
        public override string Namespace
        {
            get{ return "DAV:"; }
        }

        /// <summary>
        /// Gets property name.
        /// </summary>
        public override string Name
        {
            get{ return "resourcetype"; }
        }

        /// <summary>
        /// Gets property value.
        /// </summary>
        public override string Value
        {
            get{ 
                StringBuilder retVal = new StringBuilder();
                for(int i=0;i<m_pItems.Count;i++){
                    if(i == (m_pItems.Count - 1)){
                        retVal.Append(m_pItems[i]);
                    }
                    else{
                        retVal.Append(m_pItems[i] + ";");
                    }
                }
                
                return retVal.ToString(); 
            }
        }

        /// <summary>
        /// Gets resource types.
        /// </summary>
        public string[] ResourceTypes
        {
            get{ return m_pItems.ToArray(); }
        }

        #endregion
    }
}
