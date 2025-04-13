using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// This base class for SIP data types what has parameters support.
    /// </summary>
    public abstract class SIP_t_ValueWithParams : SIP_t_Value
    {
        private SIP_ParameterCollection m_pParameters = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_ValueWithParams()
        {
            m_pParameters = new SIP_ParameterCollection();
        }


        #region mehtod ParseParameters

        /// <summary>
        /// Parses parameters from specified reader. Reader position must be where parameters begin.
        /// </summary>
        /// <param name="reader">Reader from where to read parameters.</param>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        protected void ParseParameters(StringReader reader)
        {
            // Remove all old parameters.
            m_pParameters.Clear();

            // Parse parameters
            while(reader.Available > 0){
                reader.ReadToFirstChar();

                // We have parameter
                if(reader.SourceString.StartsWith(";")){
                    reader.ReadSpecifiedLength(1);
                    string paramString = reader.QuotedReadToDelimiter(new char[]{';',','},false);
                    if(paramString != ""){
                        string[] name_value = paramString.Split(new char[]{'='},2);
                        if(name_value.Length == 2){
                           this.Parameters.Add(name_value[0],TextUtils.UnQuoteString(name_value[1]));
                        }
                        else{
                            this.Parameters.Add(name_value[0],null);
                        }
                    }
                }
                // Next value
                else if(reader.SourceString.StartsWith(",")){
                    break;
                }
                // Unknown data
                else{
                    throw new SIP_ParseException("Unexpected value '" + reader.SourceString + "' !");
                }
            }
        }

        #endregion

        #region method ParametersToString

        /// <summary>
        /// Convert parameters to valid parameters string.
        /// </summary>
        /// <returns>Returns parameters string.</returns>
        protected string ParametersToString()
        {
            StringBuilder retVal = new StringBuilder();
            foreach(SIP_Parameter parameter in m_pParameters){
                if(!string.IsNullOrEmpty(parameter.Value)){
                    if(TextUtils.IsToken(parameter.Value)){
                        retVal.Append(";" + parameter.Name + "=" + parameter.Value);
                    }
                    else{
                        retVal.Append(";" + parameter.Name + "=" + TextUtils.QuoteString(parameter.Value));
                    }
                }
                else{
                    retVal.Append(";" + parameter.Name);
                }
            }

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets via parameters.
        /// </summary>
        public SIP_ParameterCollection Parameters
        {
            get{ return m_pParameters; }
        }

        #endregion

    }
}
