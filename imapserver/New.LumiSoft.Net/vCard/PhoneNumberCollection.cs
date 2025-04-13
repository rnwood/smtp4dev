using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Mime.vCard
{
    /// <summary>
    /// vCard phone number collection implementation.
    /// </summary>
    public class PhoneNumberCollection : IEnumerable
    {
        private vCard             m_pOwner      = null;
        private List<PhoneNumber> m_pCollection = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="owner">Owner vCard.</param>
        internal PhoneNumberCollection(vCard owner)
        {
            m_pOwner      = owner;
            m_pCollection = new List<PhoneNumber>();

            foreach(Item item in owner.Items.Get("TEL")){
                m_pCollection.Add(PhoneNumber.Parse(item));
            }
        }


        #region method Add

        /// <summary>
        /// Add new phone number to the collection.
        /// </summary>
        /// <param name="type">Phone number type. Note: This value can be flagged value !</param>
        /// <param name="number">Phone number.</param>
        public void Add(PhoneNumberType_enum type,string number)
        {            
            Item item = m_pOwner.Items.Add("TEL",PhoneNumber.PhoneTypeToString(type),number);            
            m_pCollection.Add(new PhoneNumber(item,type,number));
        }

        #endregion

        #region method Remove

        /// <summary>
        /// Removes specified item from the collection.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        public void Remove(PhoneNumber item)
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
            foreach(PhoneNumber number in m_pCollection){
                m_pOwner.Items.Remove(number.Item);
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

        #endregion

    }
}
