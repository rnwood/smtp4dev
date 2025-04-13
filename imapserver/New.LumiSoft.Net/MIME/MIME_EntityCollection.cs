using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// Represents MIME child entity collection in multipart/xxx entity.
    /// </summary>
    public class MIME_EntityCollection : IEnumerable
    {
        private bool              m_IsModified  = false;
        private List<MIME_Entity> m_pCollection = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal MIME_EntityCollection()
        {
            m_pCollection = new List<MIME_Entity>();
        }


        #region method Add
                
        /// <summary>
        /// Adds specified MIME enity to the collection.
        /// </summary>
        /// <param name="entity">MIME entity.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>entity</b> is null reference.</exception>
        public void Add(MIME_Entity entity)
        {
            if(entity == null){
                throw new ArgumentNullException("entity");
            }

            m_pCollection.Add(entity);
            m_IsModified = true;
        }

        #endregion

        #region method Insert

        /// <summary>
        /// Inserts a new MIME entity into the collection at the specified location.
        /// </summary>
        /// <param name="index">The location in the collection where you want to add the MIME entity.</param>
        /// <param name="entity">MIME entity.</param>
        /// <exception cref="IndexOutOfRangeException">Is raised when <b>index</b> is out of range.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>entity</b> is null reference.</exception>
        public void Insert(int index,MIME_Entity entity)
        {
            if(entity == null){
                throw new ArgumentNullException("entity");
            }

            m_pCollection.Insert(index,entity);
            m_IsModified = true;
        }

        #endregion

        #region method Remove

        /// <summary>
        /// Removes specified MIME entity from the collection.
        /// </summary>
        /// <param name="entity">MIME entity.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>field</b> is null reference.</exception>
        public void Remove(MIME_Entity entity)
        {
            if(entity == null){
                throw new ArgumentNullException("field");
            }

            m_pCollection.Remove(entity);
            m_IsModified = true;
        }

        /// <summary>
        /// Removes MIME entity at the specified index from the collection.
        /// </summary>
        /// <param name="index">The index of the MIME entity to remove.</param>
        /// <exception cref="IndexOutOfRangeException">Is raised when <b>index</b> is out of range.</exception>
        public void Remove(int index)
        {
            m_pCollection.RemoveAt(index);
            m_IsModified = true;
        }

        #endregion

        #region method Clear

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            m_pCollection.Clear();
            m_IsModified = true;
        }

        #endregion

        #region mehtod Contains

        /// <summary>
        /// Gets if the collection contains specified MIME entity.
        /// </summary>
        /// <param name="entity">MIME entity.</param>
        /// <returns>Returns true if the specified MIME entity exists in the collection, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>entity</b> is null.</exception>
        public bool Contains(MIME_Entity entity)
        {
            if(entity == null){
                throw new ArgumentNullException("entity");
            }

            return m_pCollection.Contains(entity);
        }

        #endregion


        #region method SetModified

        /// <summary>
        /// Sets IsModified property value.
        /// </summary>
        /// <param name="isModified">Modified flag.</param>
        internal void SetModified(bool isModified)
        {
            m_IsModified = isModified;
        }

        #endregion


        #region interface IEnumerator

        /// <summary>
		/// Gets enumerator.
		/// </summary>
		/// <returns>Returns IEnumerator interface.</returns>
		public IEnumerator GetEnumerator()
		{
			return m_pCollection.GetEnumerator();
		}

		#endregion

        #region Properties Implementation

        /// <summary>
        /// Gets if enity collection has modified.
        /// </summary>
        public bool IsModified
        {
            get{
                if(m_IsModified){
                    return true;
                }

                foreach(MIME_Entity entity in m_pCollection){
                    if(entity.IsModified){
                        return true;
                    }
                }

                return false; 
            }
        }

        /// <summary>
        /// Gets number of items in the collection.
        /// </summary>
        public int Count
        {
            get{ return m_pCollection.Count; }
        }

        /// <summary>
        /// Gets MIME entity at the specified index.
        /// </summary>
        /// <param name="index">MIME entity zero-based index.</param>
        /// <returns>Returns MIME entity.</returns>
        public MIME_Entity this[int index]
        {
            get{ return m_pCollection[index]; }
        }

        #endregion

    }
}
