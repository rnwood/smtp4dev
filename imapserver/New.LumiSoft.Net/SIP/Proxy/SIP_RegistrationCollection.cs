using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// SIP registration collection.
    /// </summary>
    public class SIP_RegistrationCollection : IEnumerable
    {
        private List<SIP_Registration> m_pRegistrations = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_RegistrationCollection()
        {
            m_pRegistrations = new List<SIP_Registration>();
        }

        
        #region method Add

        /// <summary>
        /// Adds specified registration to collection.
        /// </summary>
        /// <param name="registration">Registration to add.</param>
        public void Add(SIP_Registration registration)
        {
            lock(m_pRegistrations){
                if(Contains(registration.AOR)){
                    throw new ArgumentException("Registration with specified registration name already exists !");
                }

                m_pRegistrations.Add(registration);
            }
        }

        #endregion

        #region method Remove

        /// <summary>
        /// Deletes specified registration and all it's contacts. 
        /// </summary>
        /// <param name="addressOfRecord">Registration address of record what to remove.</param>
        public void Remove(string addressOfRecord)
        {
            lock(m_pRegistrations){
                foreach(SIP_Registration registration in m_pRegistrations){
                    if(registration.AOR.ToLower() == addressOfRecord.ToLower()){
                        m_pRegistrations.Remove(registration);
                        break;
                    }
                }
            }
        }

        #endregion

        #region method Contains

        /// <summary>
        /// Gets if registration with specified name already exists.
        /// </summary>
        /// <param name="addressOfRecord">Address of record of resgistration.</param>
        /// <returns></returns>
        public bool Contains(string addressOfRecord)
        {
            lock(m_pRegistrations){
                foreach(SIP_Registration registration in m_pRegistrations){
                    if(registration.AOR.ToLower() == addressOfRecord.ToLower()){
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion


        #region method RemoveExpired

        /// <summary>
        /// Removes all expired registrations from the collection.
        /// </summary>
        public void RemoveExpired()
        {
            lock(m_pRegistrations){
                for(int i=0;i<m_pRegistrations.Count;i++){
                    SIP_Registration registration = m_pRegistrations[i];

                    // Force registration to remove all its expired contacts.
                    registration.RemoveExpiredBindings();
                    // No bindings left, so we need to remove that registration.
                    if(registration.Bindings.Length == 0){
                        m_pRegistrations.Remove(registration);
                        i--;
                    }
                }
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
			return m_pRegistrations.GetEnumerator();
		}

		#endregion

        #region Properties implementation

        /// <summary>
        /// Gets number of registrations in the collection.
        /// </summary>
        public int Count
        {
            get{ return m_pRegistrations.Count; }
        }

        /// <summary>
        /// Gets registration with specified registration name. Returns null if specified registration doesn't exist.
        /// </summary>
        /// <param name="addressOfRecord">Address of record of resgistration.</param>
        /// <returns></returns>
        public SIP_Registration this[string addressOfRecord]
        {
            get{ 
                lock(m_pRegistrations){
                    foreach(SIP_Registration registration in m_pRegistrations){
                        if(registration.AOR.ToLower() == addressOfRecord.ToLower()){
                            return registration;
                        }
                    }
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets SIP registrations what in the collection.
        /// </summary>
        public SIP_Registration[] Values
        {
            get{ return m_pRegistrations.ToArray(); }
        }
                
        #endregion

    }
}
