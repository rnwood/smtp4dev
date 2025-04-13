using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represents MIME headers provider.
    /// </summary>
    public class MIME_h_Provider
    {
        private Type                    m_pDefaultHeaderField = null;
        private Dictionary<string,Type> m_pHeadrFields        = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MIME_h_Provider()
        {
            m_pDefaultHeaderField = typeof(MIME_h_Unstructured);

            m_pHeadrFields = new Dictionary<string,Type>(StringComparer.CurrentCultureIgnoreCase);
            m_pHeadrFields.Add("Content-Type",typeof(MIME_h_ContentType));
            m_pHeadrFields.Add("Content-Disposition",typeof(MIME_h_ContentDisposition));
        }


        #region method Parse

        /// <summary>
        /// Parses specified header field.
        /// </summary>
        /// <param name="field">Header field string (Name: value).</param>
        /// <returns>Returns parsed header field.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>field</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public MIME_h Parse(string field)
        {
            if(field == null){
                throw new ArgumentNullException("field");
            }

            // <CRLF> is misssing from end, add it.
            if(!field.EndsWith("\r\n")){
                field += "\r\n";
            }

            MIME_h   headerField = null;
            string[] name_value  = field.Split(new char[]{':'},2);
            string   name        = name_value[0].Trim();
            if(name == string.Empty){
                throw new ParseException("Invalid header field value '" + field + "'.");
            }
            else{
                try{
                    if(m_pHeadrFields.ContainsKey(name)){ 
                        headerField = (MIME_h)m_pHeadrFields[name].GetMethod("Parse").Invoke(null,new object[]{field});
                    }
                    else{
                        headerField = (MIME_h)m_pDefaultHeaderField.GetMethod("Parse").Invoke(null,new object[]{field});
                    }
                }
                catch(Exception x){
                    headerField = new MIME_h_Unparsed(field,x.InnerException);
                }
            }

            return headerField;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets or sets default header field what is used to reperesent unknown header fields.
        /// </summary>
        /// <remarks>This property value value must be based on <see cref="MIME_h"/> class.</remarks>
        /// <exception cref="ArgumentNullException">Is raised when null reference passed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public Type DefaultHeaderField
        {
            get{ return m_pDefaultHeaderField; }

            set{
                if(value == null){
                    throw new ArgumentNullException("DefaultHeaderField");
                }                
                if(!value.GetType().IsSubclassOf(typeof(MIME_h))){
                    throw new ArgumentException("Property 'DefaultHeaderField' value must be based on MIME_h class.");
                }

                m_pDefaultHeaderField = value;
            }
        }

        /// <summary>
        /// Gets header fields parsers collection.
        /// </summary>
        public Dictionary<string,Type> HeaderFields
        {
            get{ return m_pHeadrFields; }
        }

        #endregion
    }
}
