using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represents normal unstructured text header field.
    /// </summary>
    public class MIME_h_Unstructured : MIME_h
    {
        private string m_ParseValue = null;
        private string m_Name       = "";
        private string m_Value      = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">Header field name.</param>
        /// <param name="value">Header field value.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>name</b> or <b>value</b> is null reference.</exception>
        public MIME_h_Unstructured(string name,string value)
        {
            if(name == null){
                throw new ArgumentNullException("name");
            }
            if(name == string.Empty){
                throw new ArgumentException("Argument 'name' value must be specified.","name");
            }
            if(value == null){
                throw new ArgumentNullException("value");
            }

            m_Name  = name;
            m_Value = value;
        }

        /// <summary>
        /// Internal parser constructor.
        /// </summary>
        private MIME_h_Unstructured()
        {
        }


        #region static method Parse

        /// <summary>
        /// Parses header field from the specified value.
        /// </summary>
        /// <param name="value">Header field value. Header field name must be included. For example: 'Content-Type: text/plain'.</param>
        /// <returns>Returns parsed header field.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public static MIME_h_Unstructured Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            MIME_h_Unstructured retVal = new MIME_h_Unstructured();

            string[] name_value = value.Split(new char[]{':'},2);
            if(name_value[0].Trim() == string.Empty){
                throw new ParseException("Invalid header field '" + value + "' syntax.");
            }

            retVal.m_Name  = name_value[0];

            retVal.m_Value = MIME_Encoding_EncodedWord.DecodeTextS(MIME_Utils.UnfoldHeader(name_value.Length == 2 ? name_value[1].TrimStart() : ""));

            retVal.m_ParseValue = value;

            return retVal;
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns header field as string.
        /// </summary>
        /// <param name="wordEncoder">8-bit words ecnoder. Value null means that words are not encoded.</param>
        /// <param name="parmetersCharset">Charset to use to encode 8-bit characters. Value null means parameters not encoded.</param>
        /// <param name="reEncode">If true always specified encoding is used. If false and header field value not modified, original encoding is kept.</param>
        /// <returns>Returns header field as string.</returns>
        public override string ToString(MIME_Encoding_EncodedWord wordEncoder,Encoding parmetersCharset,bool reEncode)
        {
            if(!reEncode && m_ParseValue != null){
                return m_ParseValue;
            }
            else{
                if(wordEncoder != null){
                    return m_Name + ": " + wordEncoder.Encode(m_Value) + "\r\n";
                }
                else{
                    return m_Name + ": " + m_Value + "\r\n";
                }
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if this header field is modified since it has loaded.
        /// </summary>
        /// <remarks>All new added header fields has <b>IsModified = true</b>.</remarks>
        /// <exception cref="ObjectDisposedException">Is riased when this class is disposed and this property is accessed.</exception>
        public override bool IsModified
        {            
            get{ return m_ParseValue == null; }
        }

        /// <summary>
        /// Gets header field name.
        /// </summary>
        public override string Name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// Gets or sets header field value.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when when null reference is passed.</exception>
        public string Value
        {
            get{ return m_Value; }

            set{
                if(value == null){
                    throw new ArgumentNullException("value");
                }

                m_Value = value;
                // Reset parse value.
                m_ParseValue = null;
            }
        }

        #endregion

    }
}
