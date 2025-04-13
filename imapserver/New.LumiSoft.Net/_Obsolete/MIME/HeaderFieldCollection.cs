using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.Mime
{
	/// <summary>
	/// Mime entity header fields collection.
	/// </summary>
    [Obsolete("See LumiSoft.Net.MIME or LumiSoft.Net.Mail namepaces for replacement.")]
	public class HeaderFieldCollection : IEnumerable
	{
		private List<HeaderField> m_pHeaderFields = null;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public HeaderFieldCollection()
		{
			m_pHeaderFields = new List<HeaderField>();
		}

		#region method Add
        
		/// <summary>
		/// Adds a new header field with specified name and value to the end of the collection.
		/// </summary>
		/// <param name="fieldName">Header field name.</param>
		/// <param name="value">Header field value.</param>
		public void Add(string fieldName,string value)
		{
            m_pHeaderFields.Add(new HeaderField(fieldName,value));            
		}

		/// <summary>
		/// Adds specified header field to the end of the collection.
		/// </summary>
		/// <param name="headerField">Header field.</param>
		public void Add(HeaderField headerField)
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
			m_pHeaderFields.Insert(index,new HeaderField(fieldName,value));
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
		public void Remove(HeaderField field)
		{
			m_pHeaderFields.Remove(field);
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
				HeaderField h = (HeaderField)m_pHeaderFields[i];
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
			foreach(HeaderField h in m_pHeaderFields){
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
		public bool Contains(HeaderField headerField)
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
		public HeaderField GetFirst(string fieldName)
		{
			foreach(HeaderField h in m_pHeaderFields){
				if(h.Name.ToLower() == fieldName.ToLower()){
					return h;
				}
			}

			return null;
		}

		#endregion

		#region method Get

		/// <summary>
		/// Gets header fields with specified name, returns null if specified field doesn't exist.
		/// </summary>
		/// <param name="fieldName">Header field name.</param>
		/// <returns></returns>
		public HeaderField[] Get(string fieldName)
		{
			ArrayList fields = new ArrayList();
			foreach(HeaderField h in m_pHeaderFields){
				if(h.Name.ToLower() == fieldName.ToLower()){
					fields.Add(h);
				}
			}

			if(fields.Count > 0){
				HeaderField[] retVal = new HeaderField[fields.Count];
				fields.CopyTo(retVal);

				return retVal;
			}
			else{
				return null;
			}
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
            Parse(new SmartStream(stream,false));
        }

		/// <summary>
		/// Parses header fields from stream. Stream position stays where header reading ends.
		/// </summary>
		/// <param name="stream">Stream from where to parse.</param>
		public void Parse(SmartStream stream)
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

            SmartStream.ReadLineAsyncOP args = new SmartStream.ReadLineAsyncOP(new byte[32000],SizeExceededAction.JunkAndThrowException);
            stream.ReadLine(args,false);
            if(args.Error != null){
                throw args.Error;
            }
            string line = args.LineUtf8;

			while(line != null){
				// End of header reached
				if(line == ""){
					break;
				}

				// Store current header line and read next. We need to read 1 header line to ahead,
				// because of multiline header fields.
				string headerField = line; 
				stream.ReadLine(args,false);
                if(args.Error != null){
                    throw args.Error;
                }
                line = args.LineUtf8;

				// See if header field is multiline. See comment above.				
				while(line != null && (line.StartsWith("\t") || line.StartsWith(" "))){
					headerField += line;
					stream.ReadLine(args,false);
                    if(args.Error != null){
                        throw args.Error;
                    }
                    line = args.LineUtf8;
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
		/// Converts header fields to rfc 2822 message header string.
		/// </summary>
		/// <param name="encodingCharSet">CharSet to use for non ASCII header field values. Utf-8 is recommended value, if you explicity don't need other.</param>
		/// <returns></returns>
		public string ToHeaderString(string encodingCharSet)
		{
			StringBuilder headerString = new StringBuilder();
			foreach(HeaderField f in this){                
				headerString.Append(f.Name + " " + f.EncodedValue + "\r\n");
			}

			return headerString.ToString();
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
		public HeaderField this[int index]
		{
			get{ return (HeaderField)m_pHeaderFields[index]; }
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
