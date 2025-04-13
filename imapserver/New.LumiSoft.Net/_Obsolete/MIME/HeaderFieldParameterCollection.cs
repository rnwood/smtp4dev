using System;
using System.Collections;

namespace LumiSoft.Net.Mime
{
	/// <summary>
	/// Header field parameters collection.
	/// </summary>
    [Obsolete("See LumiSoft.Net.MIME or LumiSoft.Net.Mail namepaces for replacement.")]
	public class HeaderFieldParameterCollection : IEnumerable
	{
		private ParametizedHeaderField m_pHeaderField = null;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="headerField">Header field.</param>
		internal HeaderFieldParameterCollection(ParametizedHeaderField headerField)
		{
			m_pHeaderField = headerField;
		}

		#region method Add

		/// <summary>
		/// Adds a new header field parameter with specified name and value to the end of the collection.
		/// </summary>
		/// <param name="parameterName">Parameter name.</param>
		/// <param name="parameterValue">Parameter value.</param>
		public void Add(string parameterName,string parameterValue)
		{
			parameterName = parameterName.ToLower();

			Hashtable parameters = m_pHeaderField.ParseParameters();
			if(!parameters.ContainsKey(parameterName)){
				parameters.Add(parameterName,parameterValue);

				m_pHeaderField.StoreParameters(m_pHeaderField.Value,parameters);
			}
			else{
				throw new Exception("Header field '" + m_pHeaderField.Name + "' parameter '" + parameterName + "' already exists !");
			}
		}

		#endregion


		#region method Remove

		/// <summary>
		/// Removes specified header field parameter from the collection.
		/// </summary>
		/// <param name="parameterName">The name of the header field parameter to remove.</param>
		public void Remove(string parameterName)
		{
			parameterName = parameterName.ToLower();

			Hashtable parameters = m_pHeaderField.ParseParameters();
			if(!parameters.ContainsKey(parameterName)){
				parameters.Remove(parameterName);

				m_pHeaderField.StoreParameters(m_pHeaderField.Value,parameters);
			}
		}

		#endregion

		#region method Clear

		/// <summary>
		/// Clears the collection of all header field parameters.
		/// </summary>
		public void Clear()
		{
			Hashtable parameters = m_pHeaderField.ParseParameters();
			parameters.Clear();
			m_pHeaderField.StoreParameters(m_pHeaderField.Value,parameters);
		}

		#endregion


		#region method Contains

		/// <summary>
		/// Gets if collection contains specified parameter.
		/// </summary>
		/// <param name="parameterName">Parameter name.</param>
		/// <returns></returns>
		public bool Contains(string parameterName)
		{
			parameterName = parameterName.ToLower();

			Hashtable parameters = m_pHeaderField.ParseParameters();
			return parameters.ContainsKey(parameterName);
		}

		#endregion


		#region interface IEnumerator

		/// <summary>
		/// Gets enumerator.
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			Hashtable parameters = m_pHeaderField.ParseParameters();
			HeaderFieldParameter[] retVal = new HeaderFieldParameter[parameters.Count];
			int i = 0;
			foreach(DictionaryEntry entry in parameters){
				retVal[i] = new HeaderFieldParameter(entry.Key.ToString(),entry.Value.ToString());
				i++;
			}

			return retVal.GetEnumerator();
		}

		#endregion

		#region Properties Implementation

		/// <summary>
		/// Gets or sets specified parameter value.
		/// </summary>
		public string this[string parameterName]
		{
			get{ 
				parameterName = parameterName.ToLower();

				Hashtable parameters = m_pHeaderField.ParseParameters();
				if(!parameters.ContainsKey(parameterName)){
					throw new Exception("Specified parameter '" + parameterName + "' doesn't exist !");
				}
				else{
					return (string)parameters[parameterName];
				}
			}

			set{
				parameterName = parameterName.ToLower();

				Hashtable parameters = m_pHeaderField.ParseParameters();
				if(parameters.ContainsKey(parameterName)){
					parameters[parameterName] = value;

					m_pHeaderField.StoreParameters(m_pHeaderField.Value,parameters);
				}
			}
		}

		/// <summary>
		/// Gets header field parameters count in the collection.
		/// </summary>
		public int Count
		{
			get{ return m_pHeaderField.ParseParameters().Count; }
		}

		#endregion

	}
}
