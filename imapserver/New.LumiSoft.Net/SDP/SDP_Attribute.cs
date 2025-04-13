using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SDP
{
    /// <summary>
    /// Implements SDP attribute.
    /// </summary>
    public class SDP_Attribute
    {
        private string m_Name  = "";
        private string m_Value = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">Attribute name.</param>
        /// <param name="value">Attribute value.</param>
        public SDP_Attribute(string name,string value)
        {
            m_Name     = name;
            this.Value = value;
        }


        #region method static Parse

        /// <summary>
        /// Parses media from "a" SDP message field.
        /// </summary>
        /// <param name="aValue">"a" SDP message field.</param>
        /// <returns></returns>
        public static SDP_Attribute Parse(string aValue)
        {
            // a=<attribute>
            // a=<attribute>:<value>

            // Remove a=
            StringReader r = new StringReader(aValue);
            r.QuotedReadToDelimiter('=');

            //--- <attribute> ------------------------------------------------------------
            string name = "";
            string word = r.QuotedReadToDelimiter(':');
            if(word == null){
                throw new Exception("SDP message \"a\" field <attribute> name is missing !");
            }
            name = word;

            //--- <value> ----------------------------------------------------------------
            string value ="";
            word = r.ReadToEnd();
            if(word != null){
                value = word;
            }

            return new SDP_Attribute(name,value);
        }

        #endregion

        #region method ToValue

        /// <summary>
        /// Converts this to valid "a" string.
        /// </summary>
        /// <returns></returns>
        public string ToValue()
        {
            // a=<attribute>
            // a=<attribute>:<value>

            // a=<attribute>
            if(string.IsNullOrEmpty(m_Value)){
                return "a=" + m_Name + "\r\n";
            }
            // a=<attribute>:<value>
            else{
                return "a=" + m_Name + ":" + m_Value + "\r\n";
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets attribute name.
        /// </summary>
        public string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets or sets attribute value.
        /// </summary>
        public string Value
        {
            get{ return m_Value; }

            set{ m_Value = value; }
        }

        #endregion

    }
}
