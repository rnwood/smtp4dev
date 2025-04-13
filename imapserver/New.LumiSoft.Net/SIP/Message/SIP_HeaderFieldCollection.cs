using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
	/// SIP header fields collection.
	/// </summary>
	public class SIP_HeaderFieldCollection : IEnumerable
	{
		private List<SIP_HeaderField> m_pHeaderFields = null;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SIP_HeaderFieldCollection()
		{
			m_pHeaderFields = new List<SIP_HeaderField>();
		}


		#region method Add
        
		/// <summary>
		/// Adds a new header field with specified name and value to the end of the collection.
		/// </summary>
		/// <param name="fieldName">Header field name.</param>
		/// <param name="value">Header field value.</param>
		public void Add(string fieldName,string value)
		{
            Add(GetheaderField(fieldName,value));
		}

		/// <summary>
		/// Adds specified header field to the end of the collection.
		/// </summary>
		/// <param name="headerField">Header field.</param>
		public void Add(SIP_HeaderField headerField)
		{
			m_pHeaderFields.Add(headerField);
		}

		#endregion

		#region method Insert

		/// <summary>
		/// Inserts a new header field into the collection at the specified location.
		/// </summary>
		/// <param name="index">The location in the collection where you want to add the header field.</param>
		/// <param name="fieldName">Header field name.</param>
		/// <param name="value">Header field value.</param>
		public void Insert(int index,string fieldName,string value)
		{
            m_pHeaderFields.Insert(index,GetheaderField(fieldName,value));
		}

		#endregion

        #region method Set

        /// <summary>
        /// Sets specified header field value. If header field existst, first found value is set.
        /// If field doesn't exist, it will be added.
        /// </summary>
        /// <param name="fieldName">Header field name.</param>
        /// <param name="value">Header field value.</param>
        public void Set(string fieldName,string value)
        {
            SIP_HeaderField h = this.GetFirst(fieldName);
            if(h != null){
                h.Value = value;
            }
            else{
                this.Add(fieldName,value);
            }
        }

        #endregion


        #region method Remove

        /// <summary>
		/// Removes header field at the specified index from the collection.
		/// </summary>
		/// <param name="index">The index of the header field to remove.</param>
		public void Remove(int index)
		{
			m_pHeaderFields.RemoveAt(index);
		}

		/// <summary>
		/// Removes specified header field from the collection.
		/// </summary>
		/// <param name="field">Header field to remove.</param>
		public void Remove(SIP_HeaderField field)
		{
			m_pHeaderFields.Remove(field);
		}

		#endregion

        #region method RemoveFirst

        /// <summary>
        /// Removes first header field with specified name.
        /// </summary>
        /// <param name="name">Header fields name.</param>
        public void RemoveFirst(string name)
        {
            foreach(SIP_HeaderField h in m_pHeaderFields){
                if(h.Name.ToLower() == name.ToLower()){
                    m_pHeaderFields.Remove(h);
                    break;
                }
            }
        }

        #endregion

        #region method RemoveAll

        /// <summary>
		/// Removes all header fields with specified name from the collection.
		/// </summary>
		/// <param name="fieldName">Header field name.</param>
		public void RemoveAll(string fieldName)
		{
			for(int i=0;i<m_pHeaderFields.Count;i++){
				SIP_HeaderField h = (SIP_HeaderField)m_pHeaderFields[i];
				if(h.Name.ToLower() == fieldName.ToLower()){
					m_pHeaderFields.Remove(h);
					i--;
				}
			}
		}

		#endregion
		
		#region method Clear

		/// <summary>
		/// Clears the collection of all header fields.
		/// </summary>
		public void Clear()
		{
			m_pHeaderFields.Clear();
		}

		#endregion


		#region method Contains

		/// <summary>
		/// Gets if collection contains specified header field.
		/// </summary>
		/// <param name="fieldName">Header field name.</param>
		/// <returns></returns>
		public bool Contains(string fieldName)
		{
			foreach(SIP_HeaderField h in m_pHeaderFields){
				if(h.Name.ToLower() == fieldName.ToLower()){
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Gets if collection contains specified header field.
		/// </summary>
		/// <param name="headerField">Header field.</param>
		/// <returns></returns>
		public bool Contains(SIP_HeaderField headerField)
		{
			return m_pHeaderFields.Contains(headerField);
		}

		#endregion


		#region method GetFirst

		/// <summary>
		/// Gets first header field with specified name, returns null if specified field doesn't exist.
		/// </summary>
		/// <param name="fieldName">Header field name.</param>
		/// <returns></returns>
		public SIP_HeaderField GetFirst(string fieldName)
		{
			foreach(SIP_HeaderField h in m_pHeaderFields){
				if(h.Name.ToLower() == fieldName.ToLower()){
					return h;
				}
			}

			return null;
		}

		#endregion

		#region method Get

		/// <summary>
		/// Gets header fields with specified name.
		/// </summary>
		/// <param name="fieldName">Header field name.</param>
		/// <returns></returns>
		public SIP_HeaderField[] Get(string fieldName)
		{
            List<SIP_HeaderField> fields = new List<SIP_HeaderField>();
			foreach(SIP_HeaderField h in m_pHeaderFields){
				if(h.Name.ToLower() == fieldName.ToLower()){
					fields.Add(h);
				}
			}

            return fields.ToArray();
		}

		#endregion


        #region method Parse
	
		/// <summary>
		/// Parses header fields from string.
		/// </summary>
		/// <param name="headerString">Header string.</param>
		public void Parse(string headerString)
		{
			Parse(new MemoryStream(Encoding.Default.GetBytes(headerString)));
		}

		/// <summary>
		/// Parses header fields from stream. Stream position stays where header reading ends.
		/// </summary>
		/// <param name="stream">Stream from where to parse.</param>
		public void Parse(Stream stream)
		{			
			/* Rfc 2822 2.2 Header Fields
				Header fields are lines composed of a field name, followed by a colon
				(":"), followed by a field body, and terminated by CRLF.  A field
				name MUST be composed of printable US-ASCII characters (i.e.,
				characters that have values between 33 and 126, inclusive), except
				colon.  A field body may be composed of any US-ASCII characters,
				except for CR and LF.  However, a field body may contain CRLF when
				used in header "folding" and  "unfolding" as described in section
				2.2.3.  All field bodies MUST conform to the syntax described in
				sections 3 and 4 of this standard. 
				
			   Rfc 2822 2.2.3 Long Header Fields
				The process of moving from this folded multiple-line representation
				of a header field to its single line representation is called
				"unfolding". Unfolding is accomplished by simply removing any CRLF
				that is immediately followed by WSP.  Each header field should be
				treated in its unfolded form for further syntactic and semantic
				evaluation.
				
				Example:
					Subject: aaaaa<CRLF>
					<TAB or SP>aaaaa<CRLF>
			*/

			m_pHeaderFields.Clear();

			StreamLineReader r = new StreamLineReader(stream);
            r.CRLF_LinesOnly = false;
			string line = r.ReadLineString();
			while(line != null){
				// End of header reached
				if(line == ""){
					break;
				}

				// Store current header line and read next. We need to read 1 header line to ahead,
				// because of multiline header fields.
				string headerField = line; 
				line = r.ReadLineString();

				// See if header field is multiline. See comment above.				
				while(line != null && (line.StartsWith("\t") || line.StartsWith(" "))){
					headerField += line;
					line = r.ReadLineString();
				}

				string[] name_value = headerField.Split(new char[]{':'},2);
				// There must be header field name and value, otherwise invalid header field
				if(name_value.Length == 2){
			        Add(name_value[0] + ":",name_value[1].Trim());
                }
			}
		}
		
		#endregion

        #region method ToHeaderString

		/// <summary>
		/// Converts header fields to SIP message header string.
		/// </summary>
		/// <returns>Returns SIP message header as string.</returns>
		public string ToHeaderString()
		{
			StringBuilder headerString = new StringBuilder();
			foreach(SIP_HeaderField f in this){                
				headerString.Append(f.Name + " " + f.Value + "\r\n");
			}
            headerString.Append("\r\n");

			return headerString.ToString();
		}

		#endregion

        
        #region method GetheaderField

        /// <summary>
        /// Gets right type header field.
        /// </summary>
        /// <param name="name">Header field name.</param>
        /// <param name="value">Header field name.</param>
        /// <returns>Returns right type header field.</returns>
        private SIP_HeaderField GetheaderField(string name,string value)
        {
            string nameLower = name.Replace(":","").ToLower().Trim();

            //--- Replace short names to long -------//
            if(nameLower == "i"){
                nameLower = "call-id";
            }
            else if(nameLower == "m"){
                nameLower = "contact";
            }
            else if(nameLower == "e"){
                nameLower = "content-encoding";
            }
            else if(nameLower == "l"){
                nameLower = "content-length";
            }
            else if(nameLower == "c"){
                nameLower = "content-yype";
            }
            else if(nameLower == "f"){
                nameLower = "from";
            }
            else if(nameLower == "s"){
                nameLower = "subject";
            }
            else if(nameLower == "k"){
                nameLower = "supported";
            }
            else if(nameLower == "t"){
                nameLower = "to";
            }
            else if(nameLower == "v"){
                nameLower = "via";
            }
            else if(nameLower == "u"){
                nameLower = "allow-events";
            }
            else if(nameLower == "r"){
                nameLower = "refer-to";
            }
            else if(nameLower == "d"){
                nameLower = "request-disposition";
            }
            else if(nameLower == "x"){
                nameLower = "session-expires";
            }
            else if(nameLower == "o"){
                nameLower = "event";
            }
            else if(nameLower == "b"){
                nameLower = "referred-by";
            }
            else if(nameLower == "a"){
                nameLower = "accept-contact";
            }
            else if(nameLower == "y"){
                nameLower = "identity";
            }
            else if(nameLower == "n"){
                nameLower = "identity-info";
            }
            else if(nameLower == "j"){
                nameLower = "reject-contact";
            }
            //--------------------------------------//
                                    
            if(nameLower == "accept"){
                return new SIP_MultiValueHF<SIP_t_AcceptRange>("Accept:",value);
            }
            else if(nameLower == "accept-contact"){
                return new SIP_MultiValueHF<SIP_t_ACValue>("Accept-Contact:",value);
            }
            else if(nameLower == "accept-encoding"){
                return new SIP_MultiValueHF<SIP_t_Encoding>("Accept-Encoding:",value);
            }
            else if(nameLower == "accept-language"){
                return new SIP_MultiValueHF<SIP_t_Language>("Accept-Language:",value);
            }
            else if(nameLower == "accept-resource-priority"){
                return new SIP_MultiValueHF<SIP_t_RValue>("Accept-Resource-Priority:",value);
            }
            else if(nameLower == "alert-info"){
                return new SIP_MultiValueHF<SIP_t_AlertParam>("Alert-Info:",value);
            }
            else if(nameLower == "allow"){
                return new SIP_MultiValueHF<SIP_t_Method>("Allow:",value);
            }
            else if(nameLower == "allow-events"){
                return new SIP_MultiValueHF<SIP_t_EventType>("Allow-Events:",value);
            }
            else if(nameLower == "authentication-info"){
                return new SIP_SingleValueHF<SIP_t_AuthenticationInfo>("Authentication-Info:",new SIP_t_AuthenticationInfo(value));
            }
            else if(nameLower == "authorization"){
                return new SIP_SingleValueHF<SIP_t_Credentials>("Authorization:",new SIP_t_Credentials(value));
            }
            else if(nameLower == "contact"){
                return new SIP_MultiValueHF<SIP_t_ContactParam>("Contact:",value);
            }
            else if(nameLower == "Content-Disposition"){
                return new SIP_SingleValueHF<SIP_t_ContentDisposition>("Content-Disposition:",new SIP_t_ContentDisposition(value));
            }
            else if(nameLower == "cseq"){
                return new SIP_SingleValueHF<SIP_t_CSeq>("CSeq:",new SIP_t_CSeq(value));
            }
            else if(nameLower == "content-encoding"){
                return new SIP_MultiValueHF<SIP_t_ContentCoding>("Content-Encoding:",value);
            }
            else if(nameLower == "content-language"){
                return new SIP_MultiValueHF<SIP_t_LanguageTag>("Content-Language:",value);
            }
            else if(nameLower == "error-info"){
                return new SIP_MultiValueHF<SIP_t_ErrorUri>("Error-Info:",value);
            }
            else if(nameLower == "event"){
                return new SIP_SingleValueHF<SIP_t_Event>("Event:",new SIP_t_Event(value));
            }
            else if(nameLower == "from"){
                return new SIP_SingleValueHF<SIP_t_From>("From:",new SIP_t_From(value));
            }
            else if(nameLower == "history-info"){
                return new SIP_MultiValueHF<SIP_t_HiEntry>("History-Info:",value);
            }
            else if(nameLower == "identity-info"){
                return new SIP_SingleValueHF<SIP_t_IdentityInfo>("Identity-Info:",new SIP_t_IdentityInfo(value));
            }
            else if(nameLower == "in-replay-to"){
                return new SIP_MultiValueHF<SIP_t_CallID>("In-Reply-To:",value);
            }
            else if(nameLower == "join"){
                return new SIP_SingleValueHF<SIP_t_Join>("Join:",new SIP_t_Join(value));
            }
            else if(nameLower == "min-se"){
                return new SIP_SingleValueHF<SIP_t_MinSE>("Min-SE:",new SIP_t_MinSE(value));
            }
            else if(nameLower == "path"){
                return new SIP_MultiValueHF<SIP_t_AddressParam>("Path:",value);
            }
            else if(nameLower == "proxy-authenticate"){
                return new SIP_SingleValueHF<SIP_t_Challenge>("Proxy-Authenticate:",new SIP_t_Challenge(value));
            }
            else if(nameLower == "proxy-authorization"){
                return new SIP_SingleValueHF<SIP_t_Credentials>("Proxy-Authorization:",new SIP_t_Credentials(value));
            }
            else if(nameLower == "proxy-require"){
                return new SIP_MultiValueHF<SIP_t_OptionTag>("Proxy-Require:",value);
            }
            else if(nameLower == "rack"){
                return new SIP_SingleValueHF<SIP_t_RAck>("RAck:",new SIP_t_RAck(value));
            }
            else if(nameLower == "reason"){
                return new SIP_MultiValueHF<SIP_t_ReasonValue>("Reason:",value);
            }
            else if(nameLower == "record-route"){
                return new SIP_MultiValueHF<SIP_t_AddressParam>("Record-Route:",value);
            }
            else if(nameLower == "refer-sub"){
                return new SIP_SingleValueHF<SIP_t_ReferSub>("Refer-Sub:",new SIP_t_ReferSub(value));
            }
            else if(nameLower == "refer-to"){
                return new SIP_SingleValueHF<SIP_t_AddressParam>("Refer-To:",new SIP_t_AddressParam(value));
            }
            else if(nameLower == "referred-by"){
                return new SIP_SingleValueHF<SIP_t_ReferredBy>("Referred-By:",new SIP_t_ReferredBy(value));
            }
            else if(nameLower == "reject-contact"){
                return new SIP_MultiValueHF<SIP_t_RCValue>("Reject-Contact:",value);
            }
            else if(nameLower == "replaces"){
                return new SIP_SingleValueHF<SIP_t_SessionExpires>("Replaces:",new SIP_t_SessionExpires(value));
            }
            else if(nameLower == "reply-to"){
                return new SIP_MultiValueHF<SIP_t_AddressParam>("Reply-To:",value);
            }
            else if(nameLower == "request-disposition"){
                return new SIP_MultiValueHF<SIP_t_Directive>("Request-Disposition:",value);
            }
            else if(nameLower == "require"){
                return new SIP_MultiValueHF<SIP_t_OptionTag>("Require:",value);
            }
            else if(nameLower == "resource-priority"){
                return new SIP_MultiValueHF<SIP_t_RValue>("Resource-Priority:",value);
            }
            else if(nameLower == "retry-after"){
                return new SIP_SingleValueHF<SIP_t_RetryAfter>("Retry-After:",new SIP_t_RetryAfter(value));
            }
            else if(nameLower == "route"){
                return new SIP_MultiValueHF<SIP_t_AddressParam>("Route:",value);
            }
            else if(nameLower == "security-client"){
                return new SIP_MultiValueHF<SIP_t_SecMechanism>("Security-Client:",value);
            }
            else if(nameLower == "security-server"){
                return new SIP_MultiValueHF<SIP_t_SecMechanism>("Security-Server:",value);
            }
            else if(nameLower == "security-verify"){
                return new SIP_MultiValueHF<SIP_t_SecMechanism>("Security-Verify:",value);
            }
            else if(nameLower == "service-route"){
                return new SIP_MultiValueHF<SIP_t_AddressParam>("Service-Route:",value);
            }
            else if(nameLower == "session-expires"){
                return new SIP_SingleValueHF<SIP_t_SessionExpires>("Session-Expires:",new SIP_t_SessionExpires(value));
            }
            else if(nameLower == "subscription-state"){
                return new SIP_SingleValueHF<SIP_t_SubscriptionState>("Subscription-State:",new SIP_t_SubscriptionState(value));
            }
            else if(nameLower == "supported"){
                return new SIP_MultiValueHF<SIP_t_OptionTag>("Supported:",value);
            }
            else if(nameLower == "target-dialog"){
                return new SIP_SingleValueHF<SIP_t_TargetDialog>("Target-Dialog:",new SIP_t_TargetDialog(value));
            }
            else if(nameLower == "timestamp"){
                return new SIP_SingleValueHF<SIP_t_Timestamp>("Timestamp:",new SIP_t_Timestamp(value));
            }
            else if(nameLower == "to"){
                return new SIP_SingleValueHF<SIP_t_To>("To:",new SIP_t_To(value));
            }
            else if(nameLower == "unsupported"){
                return new SIP_MultiValueHF<SIP_t_OptionTag>("Unsupported:",value);
            }
            else if(nameLower == "via"){
                return new SIP_MultiValueHF<SIP_t_ViaParm>("Via:",value);
            }
            else if(nameLower == "warning"){
                return new SIP_MultiValueHF<SIP_t_WarningValue>("Warning:",value);
            }
            else if(nameLower == "www-authenticate"){
                return new SIP_SingleValueHF<SIP_t_Challenge>("WWW-Authenticate:",new SIP_t_Challenge(value));
            }
            else{
                return new SIP_HeaderField(name,value);
            }
        }

        #endregion


        #region interface IEnumerator

        /// <summary>
		/// Gets enumerator.
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			return m_pHeaderFields.GetEnumerator();
		}

		#endregion

		#region Properties Implementation
		
		/// <summary>
		/// Gets header field from specified index.
		/// </summary>
		public SIP_HeaderField this[int index]
		{
			get{ return (SIP_HeaderField)m_pHeaderFields[index]; }
		}

		/// <summary>
		/// Gets header fields count in the collection.
		/// </summary>
		public int Count
		{
			get{ return m_pHeaderFields.Count; }
		}

		#endregion

	}
}
