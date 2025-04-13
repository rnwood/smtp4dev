using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Mime.vCard
{
    /// <summary>
    /// vCard name implementation.
    /// </summary>
    public class Name
    {
        private string m_LastName        = "";
        private string m_FirstName       = "";
        private string m_AdditionalNames = "";
        private string m_HonorificPrefix = "";
        private string m_HonorificSuffix = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="lastName">Last name.</param>
        /// <param name="firstName">First name.</param>
        /// <param name="additionalNames">Comma separated additional names.</param>
        /// <param name="honorificPrefix">Honorific prefix.</param>
        /// <param name="honorificSuffix">Honorific suffix.</param>
        public Name(string lastName,string firstName,string additionalNames,string honorificPrefix,string honorificSuffix)
        {
            m_LastName        = lastName;
            m_FirstName       = firstName;
            m_AdditionalNames = additionalNames;
            m_HonorificPrefix = honorificPrefix;
            m_HonorificSuffix = honorificSuffix;
        }

        /// <summary>
        /// Internal parse constructor.
        /// </summary>
        internal Name()
        {
        }


        #region method ToValueString

        /// <summary>
        /// Converts item to vCard N structure string.
        /// </summary>
        /// <returns></returns>
        public string ToValueString()
        {
            return m_LastName + ";" + m_FirstName + ";" + m_AdditionalNames + ";" + m_HonorificPrefix + ";" + m_HonorificSuffix;
        }

        #endregion


        #region internal static method Parse

        /// <summary>
        /// Parses name info from vCard N item.
        /// </summary>
        /// <param name="item">vCard N item.</param>
        internal static Name Parse(Item item)
        {       
            string[] items = item.DecodedValue.Split(';');
            Name name = new Name();
            if(items.Length >= 1){
                name.m_LastName = items[0];
            }
            if(items.Length >= 2){
                name.m_FirstName = items[1];
            }
            if(items.Length >= 3){
                name.m_AdditionalNames = items[2];
            }
            if(items.Length >= 4){
                name.m_HonorificPrefix = items[3];
            }
            if(items.Length >= 5){
                name.m_HonorificSuffix = items[4];
            }
            return name;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets last name.
        /// </summary>
        public string LastName
        {
            get{ return m_LastName; }
        }

        /// <summary>
        /// Gets first name.
        /// </summary>
        public string FirstName
        {
            get{ return m_FirstName; }
        }

        /// <summary>
        /// Gets comma separated additional names.
        /// </summary>
        public string AdditionalNames
        {
            get{ return m_AdditionalNames; }
        }

        /// <summary>
        /// Gets honorific prefix.
        /// </summary>
        public string HonorificPerfix
        {
            get{ return m_HonorificPrefix; }
        }

        /// <summary>
        /// Gets honorific suffix.
        /// </summary>
        public string HonorificSuffix
        {
            get{ return m_HonorificSuffix; }
        }

        #endregion

    }
}
