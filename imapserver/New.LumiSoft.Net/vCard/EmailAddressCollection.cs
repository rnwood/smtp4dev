using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Mime.vCard
{
    /// <summary>
    /// vCard email address collection implementation.
    /// </summary>
    public class EmailAddressCollection : IEnumerable
    {
        private vCard              m_pOwner      = null;
        private List<EmailAddress> m_pCollection = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="owner">Owner vCard.</param>
        internal EmailAddressCollection(vCard owner)
        {
            m_pOwner      = owner;
            m_pCollection = new List<EmailAddress>();
                        
            foreach(Item item in owner.Items.Get("EMAIL")){
                m_pCollection.Add(EmailAddress.Parse(item));
            }
        }


        #region method Add

        /// <summary>
        /// Add new email address to the collection.
        /// </summary>
        /// <param name="type">Email address type. Note: This value can be flagged value !</param>
        /// <param name="email">Email address.</param>
        public EmailAddress Add(EmailAddressType_enum type,string email)
        {            
            Item item = m_pOwner.Items.Add("EMAIL",EmailAddress.EmailTypeToString(type),"");
            item.SetDecodedValue(email);
            EmailAddress emailAddress = new EmailAddress(item,type,email);
            m_pCollection.Add(emailAddress);

            return emailAddress;
        }

        #endregion

        #region method Remove

        /// <summary>
        /// Removes specified item from the collection.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        public void Remove(EmailAddress item)
        {
            m_pOwner.Items.Remove(item.Item);
            m_pCollection.Remove(item);
        }

        #endregion

        #region method Clear

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            foreach(EmailAddress email in m_pCollection){
                m_pOwner.Items.Remove(email.Item);
            }
            m_pCollection.Clear();
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
        /// Gets number of items in the collection.
        /// </summary>
        public int Count
        {
            get{ return m_pCollection.Count; }
        }

        /// <summary>
        /// Gets item at the specified index.
        /// </summary>
        /// <param name="index">Index of item which to get.</param>
        /// <returns></returns>
        public EmailAddress this[int index]
        {
            get{ return m_pCollection[index]; }
        }

        #endregion

    }
}
