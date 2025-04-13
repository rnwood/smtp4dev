using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Mime.vCard
{
    /// <summary>
    /// vCard item collection.
    /// </summary>
    public class ItemCollection : IEnumerable
    {
        private List<Item> m_pItems = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal ItemCollection()
        {
            m_pItems = new List<Item>();
        }


        #region method Add

        /// <summary>
        /// Adds new vCard item to the collection.
        /// </summary>
        /// <param name="name">Item name.</param>
        /// <param name="parametes">Item parameters.</param>
        /// <param name="value">Item value.</param>
        public Item Add(string name,string parametes,string value)
        {
            Item item = new Item(name,parametes,value);
            m_pItems.Add(item);

            return item;
        }

        #endregion

        #region method Remove

        /// <summary>
        /// Removes all items with the specified name.
        /// </summary>
        /// <param name="name">Item name.</param>
        public void Remove(string name)
        {
            for(int i=0;i<m_pItems.Count;i++){
                if(m_pItems[i].Name.ToLower() == name.ToLower()){
                    m_pItems.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Removes specified item from the collection.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        public void Remove(Item item)
        {
            m_pItems.Remove(item);
        }

        #endregion

        #region method Clear

        /// <summary>
        /// Clears all items in the collection.
        /// </summary>
        public void Clear()
        {
            m_pItems.Clear();
        }

        #endregion


        #region method GetFirst

        /// <summary>
        /// Gets first item with specified name. Returns null if specified item doesn't exists.
        /// </summary>
        /// <param name="name">Item name. Name compare is case-insensitive.</param>
        /// <returns>Returns first item with specified name or null if specified item doesn't exists.</returns>
        public Item GetFirst(string name)
        {
            foreach(Item item in m_pItems){
                if(item.Name.ToLower() == name.ToLower()){
                    return item;
                }
            }

            return null;
        }

        #endregion

        #region method Get

        /// <summary>
        /// Gets items with specified name.
        /// </summary>
        /// <param name="name">Item name.</param>
        /// <returns></returns>
        public Item[] Get(string name)
        {
            List<Item> retVal = new List<Item>();
            foreach(Item item in m_pItems){
                if(item.Name.ToLower() == name.ToLower()){
                    retVal.Add(item);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method SetDecodedStringValue

        /// <summary>
        /// Sets first item with specified value.  If item doesn't exist, item will be appended to the end.
        /// If value is null, all items with specified name will be removed.
        /// Value is encoed as needed and specified item.ParametersString will be updated accordingly.
        /// </summary>
        /// <param name="name">Item name.</param>
        /// <param name="value">Item value.</param>
        public void SetDecodedValue(string name,string value)
        {
            if(value == null){
                Remove(name);
                return;
            }

            Item item = GetFirst(name);
            if(item != null){
                item.SetDecodedValue(value);
            }
            else{
                item = new Item(name,"","");
                m_pItems.Add(item);
                item.SetDecodedValue(value);
            }
        }

        #endregion

        #region method SetValue

        /// <summary>
        /// Sets first item with specified encoded value.  If item doesn't exist, item will be appended to the end.
        /// If value is null, all items with specified name will be removed.
        /// </summary>
        /// <param name="name">Item name.</param>
        /// <param name="value">Item encoded value.</param>
        public void SetValue(string name,string value)
        {
            SetValue(name,"",value);
        }

        /// <summary>
        /// Sets first item with specified name encoded value.  If item doesn't exist, item will be appended to the end.
        /// If value is null, all items with specified name will be removed.
        /// </summary>
        /// <param name="name">Item name.</param>
        /// <param name="parametes">Item parameters.</param>
        /// <param name="value">Item encoded value.</param>
        public void SetValue(string name,string parametes,string value)
        {
            if(value == null){
                Remove(name);
                return;
            }

            Item item = GetFirst(name);
            if(item != null){
                item.Value = value;
            }
            else{
                m_pItems.Add(new Item(name,parametes,value));
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
			return m_pItems.GetEnumerator();
		}

		#endregion

        #region Properties Implementation

        /// <summary>
        /// Gets number of vCard items in the collection.
        /// </summary>
        public int Count
        {
            get{ return m_pItems.Count; }
        }

        #endregion

    }
}
