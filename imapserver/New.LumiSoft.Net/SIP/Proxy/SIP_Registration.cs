using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using LumiSoft.Net.SIP.Message;
using LumiSoft.Net.SIP.Stack;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// This class implements SIP registrar registration entry. Defined in RFC 3261 10.3.
    /// </summary>
    public class SIP_Registration
    {
        private DateTime                      m_CreateTime;
        private string                        m_UserName  = "";
        private string                        m_AOR       = "";
        private List<SIP_RegistrationBinding> m_pBindings = null;
        private object                        m_pLock     = new object();

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="userName">User name who owns this registration.</param>
        /// <param name="aor">Address of record. For example: john.doe@lumisoft.ee.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>userName</b> or <b>aor</b> is null reference.</exception>
        public SIP_Registration(string userName,string aor)
        {
            if(userName == null){
                throw new ArgumentNullException("userName");
            }
            if(aor == null){
                throw new ArgumentNullException("aor");
            }
            if(aor == ""){
                throw new ArgumentException("Argument 'aor' value must be specified.");
            }

            m_UserName = userName;
            m_AOR      = aor;

            m_CreateTime = DateTime.Now;
            m_pBindings  = new List<SIP_RegistrationBinding>();
        }


        #region method GetBinding

        /// <summary>
        /// Gets matching binding. Returns null if no match.
        /// </summary>
        /// <param name="contactUri">URI to match.</param>
        /// <returns>Returns matching binding. Returns null if no match.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>contactUri</b> is null reference.</exception>
        public SIP_RegistrationBinding GetBinding(AbsoluteUri contactUri)
        {
            if(contactUri == null){
                throw new ArgumentNullException("contactUri");
            }

            lock(m_pLock){
                foreach(SIP_RegistrationBinding binding in m_pBindings){
                    if(contactUri.Equals(binding.ContactURI)){
                        return binding;
                    }
                }

                return null;
            }
        }

        #endregion

        #region method AddOrUpdateBindings

        /// <summary>
        /// Adds or updates matching bindings.
        /// </summary>
        /// <param name="flow">SIP data flow what updates this binding. This value is null if binding was not added through network or
        /// flow has disposed.</param>
        /// <param name="callID">Call-ID header field value.</param>
        /// <param name="cseqNo">CSeq header field sequence number value.</param>
        /// <param name="contacts">Contacts to add or update.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>callID</b> or <b>contacts</b> is null reference.</exception>
        public void AddOrUpdateBindings(SIP_Flow flow,string callID,int cseqNo,SIP_t_ContactParam[] contacts)
        {
            if(callID == null){
                throw new ArgumentNullException("callID");
            }
            if(cseqNo < 0){
                throw new ArgumentException("Argument 'cseqNo' value must be >= 0.");
            }
            if(contacts == null){
                throw new ArgumentNullException("contacts");
            }

            lock(m_pLock){
                foreach(SIP_t_ContactParam contact in contacts){
                    SIP_RegistrationBinding binding = GetBinding(contact.Address.Uri);
                    // Add binding.
                    if(binding == null){
                        binding = new SIP_RegistrationBinding(this,contact.Address.Uri);
                        m_pBindings.Add(binding);
                    }

                    // Update binding.
                    binding.Update(
                        flow,
                        contact.Expires == -1 ? 3600 : contact.Expires,
                        contact.QValue == -1 ? 1.0 : contact.QValue,
                        callID,
                        cseqNo
                    );
                }
            }
        }

        #endregion

        #region method RemoveBinding

        /// <summary>
        /// Removes specified binding.
        /// </summary>
        /// <param name="binding">Registration binding.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>binding</b> is null reference.</exception>
        public void RemoveBinding(SIP_RegistrationBinding binding)
        {
            if(binding == null){
                throw new ArgumentNullException("binding");
            }

            lock(m_pLock){
                m_pBindings.Remove(binding);
            }
        }

        #endregion

        #region method RemoveAllBindings

        /// <summary>
        /// Removes all this registration bindings.
        /// </summary>
        public void RemoveAllBindings()
        {
            lock(m_pLock){
                m_pBindings.Clear();
            }
        }

        #endregion

        #region method RemoveExpiredBindings

        /// <summary>
        /// Removes all expired bindings.
        /// </summary>
        public void RemoveExpiredBindings()
        {
            lock(m_pLock){
                for(int i=0;i<m_pBindings.Count;i++){
                    if(m_pBindings[i].IsExpired){
                        m_pBindings.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets time when this registration entry was created.
        /// </summary>
        public DateTime CreateTime
        {
            get{ return m_CreateTime; }
        }

        /// <summary>
        /// Gets user name who owns this registration.
        /// </summary>
        public string UserName
        {
            get{ return m_UserName; }
        }
        
        /// <summary>
        /// Gets registration address of record.
        /// </summary>
        public string AOR
        {
            get{ return m_AOR; }
        }

        /// <summary>
        /// Gets this registration priority ordered bindings.
        /// </summary>
        public SIP_RegistrationBinding[] Bindings
        {
            get{
                SIP_RegistrationBinding[] retVal = m_pBindings.ToArray();

                // Sort by qvalue, higer qvalue means higher priority.
                Array.Sort(retVal);

                return retVal; 
            }
        }

        #endregion

    }
}
