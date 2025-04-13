using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "sec-mechanism" value. Defined in RFC 3329.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3329 Syntax:
    ///     sec-mechanism    = mechanism-name *(SEMI mech-parameters)
    ///     mechanism-name   = ( "digest" / "tls" / "ipsec-ike" / "ipsec-man" / token )
    ///     mech-parameters  = ( preference / digest-algorithm / digest-qop / digest-verify / extension )
    ///     preference       = "q" EQUAL qvalue
    ///     qvalue           = ( "0" [ "." 0*3DIGIT ] ) / ( "1" [ "." 0*3("0") ] )
    ///     digest-algorithm = "d-alg" EQUAL token
    ///     digest-qop       = "d-qop" EQUAL token
    ///     digest-verify    = "d-ver" EQUAL LDQUOT 32LHEX RDQUOT
    ///     extension        = generic-param
    /// </code>
    /// </remarks>
    public class SIP_t_SecMechanism : SIP_t_ValueWithParams
    {
        private string m_Mechanism = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_SecMechanism()
        {
        }


        #region method Parse
        
        /// <summary>
        /// Parses "sec-mechanism" from specified value.
        /// </summary>
        /// <param name="value">SIP "sec-mechanism" value.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>value</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public void Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            Parse(new StringReader(value));
        }

        /// <summary>
        /// Parses "sec-mechanism" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                sec-mechanism    = mechanism-name *(SEMI mech-parameters)
                mechanism-name   = ( "digest" / "tls" / "ipsec-ike" / "ipsec-man" / token )
                mech-parameters  = ( preference / digest-algorithm / digest-qop / digest-verify / extension )
                preference       = "q" EQUAL qvalue
                qvalue           = ( "0" [ "." 0*3DIGIT ] ) / ( "1" [ "." 0*3("0") ] )
                digest-algorithm = "d-alg" EQUAL token
                digest-qop       = "d-qop" EQUAL token
                digest-verify    = "d-ver" EQUAL LDQUOT 32LHEX RDQUOT
                extension        = generic-param
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // mechanism-name
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid 'sec-mechanism', 'mechanism-name' is missing !");
            }

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "sec-mechanism" value.
        /// </summary>
        /// <returns>Returns "sec-mechanism" value.</returns>
        public override string ToStringValue()
        {
            /*
                sec-mechanism    = mechanism-name *(SEMI mech-parameters)
                mechanism-name   = ( "digest" / "tls" / "ipsec-ike" / "ipsec-man" / token )
                mech-parameters  = ( preference / digest-algorithm / digest-qop / digest-verify / extension )
                preference       = "q" EQUAL qvalue
                qvalue           = ( "0" [ "." 0*3DIGIT ] ) / ( "1" [ "." 0*3("0") ] )
                digest-algorithm = "d-alg" EQUAL token
                digest-qop       = "d-qop" EQUAL token
                digest-verify    = "d-ver" EQUAL LDQUOT 32LHEX RDQUOT
                extension        = generic-param
            */

            StringBuilder retVal = new StringBuilder();
            
            // mechanism-name
            retVal.Append(m_Mechanism);

            // Add parameters
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets security mechanism name. Defined values: "digest","tls","ipsec-ike","ipsec-man".
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid Mechanism value is passed.</exception>
        public string Mechanism
        {
            get{ return m_Mechanism; }

            set{
                if(value == null){
                    throw new ArgumentNullException("Mechanism");
                }
                if(value == ""){
                    throw new ArgumentException("Property Mechanism value may not be '' !");
                }
                if(!TextUtils.IsToken(value)){
                    throw new ArgumentException("Property Mechanism value must be 'token' !");
                }

                m_Mechanism = value;
            }
        }

        /// <summary>
        /// Gets or sets 'q' parameter value. Value -1 means not specified.
        /// </summary>
        public double Q
        {
            get{
                if(!this.Parameters.Contains("qvalue")){
                    return -1;
                }
                else{
                    return double.Parse(this.Parameters["qvalue"].Value,System.Globalization.NumberStyles.Any);
                }
            }

            set{
                if(value < 0 || value > 2){
                    throw new ArgumentException("Property QValue value must be between 0.0 and 2.0 !");
                }

                if(value < 0){
                    this.Parameters.Remove("qvalue");
                }
                else{
                    this.Parameters.Set("qvalue",value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets 'd-alg' parameter value. Value null means not specified.
        /// </summary>
        public string D_Alg
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["d-alg"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{                
                if(string.IsNullOrEmpty(value)){
                    this.Parameters.Remove("d-alg");
                }
                else{
                    this.Parameters.Set("d-alg",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets 'd-qop' parameter value. Value null means not specified.
        /// </summary>
        public string D_Qop
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["d-qop"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{                
                if(string.IsNullOrEmpty(value)){
                    this.Parameters.Remove("d-qop");
                }
                else{
                    this.Parameters.Set("d-qop",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets 'd-ver' parameter value. Value null means not specified.
        /// </summary>
        public string D_Ver
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["d-ver"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{                
                if(string.IsNullOrEmpty(value)){
                    this.Parameters.Remove("d-ver");
                }
                else{
                    this.Parameters.Set("d-ver",value);
                }
            }
        }

        #endregion

    }
}
