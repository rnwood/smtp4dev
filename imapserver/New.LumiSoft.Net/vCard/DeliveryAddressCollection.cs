using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Mime.vCard
{
    /// <summary>
    /// vCard delivery address collection implementation.
    /// </summary>
    public class DeliveryAddressCollection : IEnumerable
    {
        private vCard                 m_pOwner      = null;
        private List<DeliveryAddress> m_pCollection = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="owner">Owner vCard.</param>
        internal DeliveryAddressCollection(vCard owner)
        {
            m_pOwner      = owner;
            m_pCollection = new List<DeliveryAddress>();
                        
            foreach(Item item in owner.Items.Get("ADR")){
                m_pCollection.Add(DeliveryAddress.Parse(item));
            }
        }


        #region method Add

        /// <summary>
        /// Add new delivery address to the collection.
        /// </summary>
        /// <param name="type">Delivery address type. Note: This value can be flagged value !</param>
        /// <param name="postOfficeAddress">Post office address.</param>
        /// <param name="extendedAddress">Extended address.</param>
        /// <param name="street">Street name.</param>
        /// <param name="locality">Locality(city).</param>
        /// <param name="region">Region.</param>
        /// <param name="postalCode">Postal code.</param>
        /// <param name="country">Country.</param>
        public void Add(DeliveryAddressType_enum type,string postOfficeAddress,string extendedAddress,string street,string locality,string region,string postalCode,string country)
        {   
            string value = "" +
                postOfficeAddress + ";" +
                extendedAddress + ";" +
                street + ";" +
                locality + ";" +
                region + ";" +
                postalCode + ";" +
                country;

            Item item = m_pOwner.Items.Add("ADR",DeliveryAddress.AddressTypeToString(type),"");
            item.SetDecodedValue(value);
            m_pCollection.Add(new DeliveryAddress(item,type,postOfficeAddress,extendedAddress,street,locality,region,postalCode,country));
        }

        #endregion

        #region method Remove

        /// <summary>
        /// Removes specified item from the collection.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        public void Remove(DeliveryAddress item)
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
            foreach(DeliveryAddress email in m_pCollection){
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
        public DeliveryAddress this[int index]
        {
            get{ return m_pCollection[index]; }
        }

        #endregion

    }
}
