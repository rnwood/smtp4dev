using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// This class represents SIP value parameters collection.
    /// </summary>
    public class SIP_ParameterCollection : IEnumerable
    {
        private List<SIP_Parameter> m_pCollection = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_ParameterCollection()
        {
            m_pCollection = new List<SIP_Parameter>();
        }


        #region method Add

        /// <summary>
        /// Adds new parameter to the collection.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>name</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when 'name' is '' or parameter with specified name 
        /// already exists in the collection.</exception>
        public void Add(string name,string value)
        {
            if(name == null){
                throw new ArgumentNullException("name");
            }
            if(Contains(name)){
                throw new ArgumentException("Prameter '' with specified name already exists in the collection !");
            }

            m_pCollection.Add(new SIP_Parameter(name,value));
        }

        #endregion

        #region method Set

        /// <summary>
        /// Adds or updates specified parameter value.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        public void Set(string name,string value)
        {
            if(Contains(name)){
                this[name].Value = value;
            }
            else{
                Add(name,value);
            }
        }

        #endregion

        #region method Clear

        /// <summary>
        /// Removes all parameters from the collection.
        /// </summary>
        public void Clear()
        {
            m_pCollection.Clear();
        }

        #endregion

        #region method Remove

        /// <summary>
        /// Removes specified parameter from the collection.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        public void Remove(string name)
        {
            SIP_Parameter parameter = this[name];
            if(parameter != null){
                m_pCollection.Remove(parameter);
            }
        }

        #endregion

        #region method Contains

        /// <summary>
        /// Checks if the collection contains parameter with the specified name.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <returns>Returns true if collection contains specified parameter.</returns>
        public bool Contains(string name)
        {
            SIP_Parameter parameter = this[name];
            if(parameter != null){
                return true;
            }
            else{
                return false;
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
			return m_pCollection.GetEnumerator();
		}

		#endregion

        #region Properties Implementation

        /// <summary>
        /// Gets parameters count in the collection.
        /// </summary>
        public int Count
        {
            get{ return m_pCollection.Count; }
        }

        /// <summary>
        /// Gets specified parameter from collection. Returns null if parameter with specified name doesn't exist.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <returns>Returns parameter with specified name or null if not found.</returns>
        public SIP_Parameter this[string name]
        {
            get{ 
                foreach(SIP_Parameter parameter in m_pCollection){
                    if(parameter.Name.ToLower() == name.ToLower()){
                        return parameter;
                    }
                }

                return null; 
            }
        }


        #endregion

    }
}
