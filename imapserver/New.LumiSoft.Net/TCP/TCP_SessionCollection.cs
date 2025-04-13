using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net.TCP
{
    /// <summary>
    /// This class implements TCP session collection.
    /// </summary>
    public class TCP_SessionCollection<T> where T : TCP_Session
    {
        private Dictionary<string,T>    m_pItems            = null;
        private Dictionary<string,long> m_pConnectionsPerIP = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal TCP_SessionCollection()
        {
            m_pItems = new Dictionary<string,T>();
            m_pConnectionsPerIP = new Dictionary<string,long>();
        }


        #region method Add

        /// <summary>
        /// Adds specified TCP session to the colletion.
        /// </summary>
        /// <param name="session">TCP server session to add.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>session</b> is null.</exception>
        internal void Add(T session)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }

            lock(m_pItems){
                m_pItems.Add(session.ID,session);

                if(session.IsConnected && session.RemoteEndPoint != null){
                    // Increase connections per IP.
                    if(m_pConnectionsPerIP.ContainsKey(session.RemoteEndPoint.Address.ToString())){
                        m_pConnectionsPerIP[session.RemoteEndPoint.Address.ToString()]++;
                    }
                    // Just add new entry for that IP address.
                    else{
                        m_pConnectionsPerIP.Add(session.RemoteEndPoint.Address.ToString(),1);
                    }
                }
            }
        }

        #endregion

        #region method Remove

        /// <summary>
        /// Removes specified TCP server session from the collection.
        /// </summary>
        /// <param name="session">TCP server session to remove.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>session</b> is null.</exception>
        internal void Remove(T session)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }

            lock(m_pItems){
                m_pItems.Remove(session.ID);

                // Decrease connections per IP.
                if(session.IsConnected && m_pConnectionsPerIP.ContainsKey(session.RemoteEndPoint.Address.ToString())){
                    m_pConnectionsPerIP[session.RemoteEndPoint.Address.ToString()]--;

                    // Last IP, so remove that IP entry.
                    if(m_pConnectionsPerIP[session.RemoteEndPoint.Address.ToString()] == 0){
                        m_pConnectionsPerIP.Remove(session.RemoteEndPoint.Address.ToString());
                    }
                }                
                
            }
        }

        #endregion

        #region method Clear

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        internal void Clear()
        {
            lock(m_pItems){
                m_pItems.Clear();
                m_pConnectionsPerIP.Clear();
            }
        }

        #endregion
        
        #region method ToArray

        /// <summary>
        /// Copies all TCP server session to new array. This method is thread-safe.
        /// </summary>
        /// <returns>Returns TCP sessions array.</returns>
        public T[] ToArray()
        {
            lock(m_pItems){
                T[] retVal = new T[m_pItems.Count];
                m_pItems.Values.CopyTo(retVal,0);

                return retVal;
            }
        }

        #endregion

        #region mehtod GetConnectionsPerIP

        /// <summary>
        /// Gets number of connections per specified IP.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <returns>Returns current number of connections of the specified IP.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null reference.</exception>
        public long GetConnectionsPerIP(IPAddress ip)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }

            long retVal = 0;
            m_pConnectionsPerIP.TryGetValue(ip.ToString(),out retVal);

            return retVal;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets number of items in the collection.
        /// </summary>
        public int Count
        {
            get{ return m_pItems.Count; }
        }

        /// <summary>
        /// Gets TCP session with the specified ID.
        /// </summary>
        /// <param name="id">Session ID.</param>
        /// <returns>Returns TCP session with the specified ID.</returns>
        public T this[string id]
        {
            get{ return m_pItems[id]; }
        }

        #endregion

    }
}
