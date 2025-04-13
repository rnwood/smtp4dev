using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Net;

using LumiSoft.Net.SIP.Message;
using LumiSoft.Net.SIP.Stack;

namespace LumiSoft.Net.SIP.Proxy
{
    #region Delegates

    /// <summary>
    /// Represents the method that will handle the SIP_Registrar.CanRegister event.
    /// </summary>
    /// <param name="userName">Authenticated user name.</param>
    /// <param name="address">Address to be registered.</param>
    /// <returns>Returns true if specified user can register specified address, otherwise false.</returns>
    public delegate bool SIP_CanRegisterEventHandler(string userName,string address);

    #endregion

    /// <summary>
    /// This class implements SIP registrar server. Defined in RFC 3261 10.3.
    /// </summary>
    public class SIP_Registrar
    {
        private bool                       m_IsDisposed     = false;
        private SIP_Proxy                  m_pProxy         = null;
        private SIP_Stack                  m_pStack         = null;
        private SIP_RegistrationCollection m_pRegistrations = null;
        private Timer                      m_pTimer         = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="proxy">Owner proxy.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>proxy</b> is null reference.</exception>
        internal SIP_Registrar(SIP_Proxy proxy)
        {
            if(proxy == null){
                throw new ArgumentNullException("proxy");
            }

            m_pProxy = proxy;
            m_pStack = m_pProxy.Stack;

            m_pRegistrations = new SIP_RegistrationCollection();

            m_pTimer = new Timer(15000);
            m_pTimer.Elapsed += new ElapsedEventHandler(m_pTimer_Elapsed);
            m_pTimer.Enabled = true;
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        internal void Dispose()
        {
            if(m_IsDisposed){
                return;
            }
            m_IsDisposed = true;

            this.CanRegister     = null;
            this.AorRegistered   = null;
            this.AorUnregistered = null;
            this.AorUpdated      = null;

            m_pProxy = null;
            m_pStack = null;
            m_pRegistrations = null;
            if(m_pTimer != null){
                m_pTimer.Dispose();
                m_pTimer = null;
            }
        }

        #endregion


        #region method m_pTimer_Elapsed

        private void m_pTimer_Elapsed(object sender,ElapsedEventArgs e)
        {
            m_pRegistrations.RemoveExpired();
        }

        #endregion


        #region method GetRegistration

        /// <summary>
        /// Gets specified registration. Returns null if no such registration.
        /// </summary>
        /// <param name="aor">Address of record of registration which to get.</param>
        /// <returns>Returns SIP registration or null if no match.</returns>
        public SIP_Registration GetRegistration(string aor)
        {
            return m_pRegistrations[aor];
        }

        #endregion

        #region method SetRegistration

        /// <summary>
        /// Add or updates specified SIP registration info.
        /// </summary>
        /// <param name="aor">Registration address of record.</param>
        /// <param name="contacts">Registration address of record contacts to update.</param>
        public void SetRegistration(string aor,SIP_t_ContactParam[] contacts)
        {
            SetRegistration(aor,contacts,null);
        }

        /// <summary>
        /// Add or updates specified SIP registration info.
        /// </summary>
        /// <param name="aor">Registration address of record.</param>
        /// <param name="contacts">Registration address of record contacts to update.</param>
        /// <param name="flow">SIP proxy local data flow what accpeted this contact registration.</param>
        public void SetRegistration(string aor,SIP_t_ContactParam[] contacts,SIP_Flow flow)
        {
            lock(m_pRegistrations){
                SIP_Registration registration = m_pRegistrations[aor];
                if(registration == null){
                    registration = new SIP_Registration("system",aor);
                    m_pRegistrations.Add(registration);
                    OnAorRegistered(registration);
                }

                registration.AddOrUpdateBindings(flow,"",1,contacts);
            }
        }

        #endregion

        #region method DeleteRegistration

        /// <summary>
        /// Deletes specified registration and all it's contacts.
        /// </summary>
        /// <param name="addressOfRecord">Registration address of record what to remove.</param>
        public void DeleteRegistration(string addressOfRecord)
        {
            m_pRegistrations.Remove(addressOfRecord);
        }

        #endregion


        #region method Register

        /// <summary>
        /// Handles REGISTER method.
        /// </summary>
        /// <param name="e">Request event arguments.</param>
        internal void Register(SIP_RequestReceivedEventArgs e)
        {
            /* RFC 3261 10.3 Processing REGISTER Requests.
                1. The registrar inspects the Request-URI to determine whether it
                   has access to bindings for the domain identified in the
                   Request-URI.  If not, and if the server also acts as a proxy
                   server, the server SHOULD forward the request to the addressed
                   domain, following the general behavior for proxying messages
                   described in Section 16.
                      
                2. To guarantee that the registrar supports any necessary extensions, 
                   the registrar MUST process the Require header field.
                     
                3. A registrar SHOULD authenticate the UAC.
                     
                4. The registrar SHOULD determine if the authenticated user is
                   authorized to modify registrations for this address-of-record.
                   For example, a registrar might consult an authorization
                   database that maps user names to a list of addresses-of-record
                   for which that user has authorization to modify bindings.  If
                   the authenticated user is not authorized to modify bindings,
                   the registrar MUST return a 403 (Forbidden) and skip the
                   remaining steps.
                     
                5. The registrar extracts the address-of-record from the To header
                   field of the request.  If the address-of-record is not valid
                   for the domain in the Request-URI, the registrar MUST send a
                   404 (Not Found) response and skip the remaining steps.  The URI
                   MUST then be converted to a canonical form.  To do that, all
                   URI parameters MUST be removed (including the user-param), and
                   any escaped characters MUST be converted to their unescaped
                   form.  The result serves as an index into the list of bindings.
                                 
                6. The registrar checks whether the request contains the Contact
                   header field.  If not, it skips to the last step.  If the
                   Contact header field is present, the registrar checks if there
                   is one Contact field value that contains the special value "*"
                   and an Expires field.  If the request has additional Contact
                   fields or an expiration time other than zero, the request is
                   invalid, and the server MUST return a 400 (Invalid Request) and
                   skip the remaining steps.  If not, the registrar checks whether
                   the Call-ID agrees with the value stored for each binding.  If
                   not, it MUST remove the binding.  If it does agree, it MUST
                   remove the binding only if the CSeq in the request is higher
                   than the value stored for that binding.  Otherwise, the update
                   MUST be aborted and the request fails.
                     
                7. The registrar now processes each contact address in the Contact
                   header field in turn.  For each address, it determines the
                   expiration interval as follows:

                     -  If the field value has an "expires" parameter, that value
                        MUST be taken as the requested expiration.

                     -  If there is no such parameter, but the request has an
                        Expires header field, that value MUST be taken as the requested expiration.

                     -  If there is neither, a locally-configured default value MUST
                        be taken as the requested expiration.

                   The registrar MAY choose an expiration less than the requested
                   expiration interval.  If and only if the requested expiration
                   interval is greater than zero AND smaller than one hour AND
                   less than a registrar-configured minimum, the registrar MAY
                   reject the registration with a response of 423 (Interval Too
                   Brief).  This response MUST contain a Min-Expires header field
                   that states the minimum expiration interval the registrar is
                   willing to honor.  It then skips the remaining steps.
              
                   For each address, the registrar then searches the list of
                   current bindings using the URI comparison rules.  If the
                   binding does not exist, it is tentatively added.  If the
                   binding does exist, the registrar checks the Call-ID value.  If
                   the Call-ID value in the existing binding differs from the
                   Call-ID value in the request, the binding MUST be removed if
                   the expiration time is zero and updated otherwise.  If they are
                   the same, the registrar compares the CSeq value.  If the value
                   is higher than that of the existing binding, it MUST update or
                   remove the binding as above.  If not, the update MUST be
                   aborted and the request fails.
    
                   This algorithm ensures that out-of-order requests from the same
                   UA are ignored.

                   Each binding record records the Call-ID and CSeq values from
                   the request.

                   The binding updates MUST be committed (that is, made visible to
                   the proxy or redirect server) if and only if all binding
                   updates and additions succeed.  If any one of them fails (for
                   example, because the back-end database commit failed), the
                   request MUST fail with a 500 (Server Error) response and all
                   tentative binding updates MUST be removed.
                     
                8. The registrar returns a 200 (OK) response.  The response MUST
                   contain Contact header field values enumerating all current
                   bindings.  Each Contact value MUST feature an "expires"
                   parameter indicating its expiration interval chosen by the
                   registrar.  The response SHOULD include a Date header field.
            */

            SIP_ServerTransaction transaction = e.ServerTransaction;
            SIP_Request           request     = e.Request;
            SIP_Uri               to          = null;
            string                userName    = "";


            // Probably we need to do validate in SIP stack.

            #region Validate request

            if(SIP_Utils.IsSipOrSipsUri(request.To.Address.Uri.ToString())){
                to = (SIP_Uri)request.To.Address.Uri;
            }
            else{
                transaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x400_Bad_Request + ": To: value must be SIP or SIPS URI.",request));
                return;
            }

            #endregion


            #region 1. Check if we are responsible for Request-URI domain

            // if(m_pProxy.OnIsLocalUri(e.Request.Uri)){
            // }
            // TODO:

            #endregion

            #region 2. Check that all required extentions supported

            #endregion

            #region 3. Authenticate request

            if(!m_pProxy.AuthenticateRequest(e,out userName)){
                return;
            }

            #endregion

            #region 4. Check if user user is authorized to modify registrations

            // We do this in next step(5.).

            #endregion

            #region 5. Check if address of record exists
            
            if(!m_pProxy.OnAddressExists(to.Address)){
                transaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x404_Not_Found,request));
                return;
            }
            else if(!OnCanRegister(userName,to.Address)){
                transaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x403_Forbidden,request));
                return;
            }

            #endregion

            #region 6. Process * Contact if exists

            // Check if we have star contact.
            SIP_t_ContactParam starContact = null;
            foreach(SIP_t_ContactParam c in request.Contact.GetAllValues()){
                if(c.IsStarContact){
                    starContact = c;
                    break;
                }
            }

            // We have star contact.
            if(starContact != null){
                if(request.Contact.GetAllValues().Length > 1){
                    transaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x400_Bad_Request + ": RFC 3261 10.3.6 -> If star(*) present, only 1 contact allowed.",request));
                    return;
                }
                else if(starContact.Expires != 0){
                    transaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x400_Bad_Request + ": RFC 3261 10.3.6 -> star(*) contact parameter 'expires' value must be always '0'.",request));
                    return;
                }

                // Remove bindings.
                SIP_Registration reg = m_pRegistrations[to.Address];
                if(reg != null){
                    foreach(SIP_RegistrationBinding b in reg.Bindings){
                        if(request.CallID != b.CallID || request.CSeq.SequenceNumber > b.CSeqNo){
                            b.Remove();
                        }
                    }
                }
            }

            #endregion

            #region 7. Process Contact values

            if(starContact == null){
                bool             newReg = false;
                SIP_Registration reg    = m_pRegistrations[to.Address];
                if(reg == null){
                    newReg = true;
                    reg    = new SIP_Registration(userName,to.Address);
                    m_pRegistrations.Add(reg);
                }

                // We may do updates in batch only.
                // We just validate all values then do update(this ensures that update doesn't fail).

                // Check expires and CSeq.
                foreach(SIP_t_ContactParam c in request.Contact.GetAllValues()){
                    if(c.Expires == -1){
                        c.Expires = request.Expires;
                    }
                    if(c.Expires == -1){
                        c.Expires = m_pProxy.Stack.MinimumExpireTime;
                    }
                    // We must accept 0 values - means remove contact.
                    if(c.Expires != 0 && c.Expires < m_pProxy.Stack.MinimumExpireTime){
                        SIP_Response resp = m_pStack.CreateResponse(SIP_ResponseCodes.x423_Interval_Too_Brief,request);
                        resp.MinExpires = m_pProxy.Stack.MinimumExpireTime;
                        transaction.SendResponse(resp);
                        return;
                    }

                    SIP_RegistrationBinding currentBinding = reg.GetBinding(c.Address.Uri);
                    if(currentBinding != null && currentBinding.CallID == request.CallID && request.CSeq.SequenceNumber < currentBinding.CSeqNo){
                        transaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x400_Bad_Request + ": CSeq value out of order.",request));
                        return;
                    }                    
                }

                // Do binding updates.
                reg.AddOrUpdateBindings(e.ServerTransaction.Flow,request.CallID,request.CSeq.SequenceNumber,request.Contact.GetAllValues());

                // Raise AOR change events.
                if(newReg){
                    OnAorRegistered(reg);
                }
                else{
                    OnAorUpdated(reg);
                }
            }

            #endregion

            #region 8. Create 200 OK response and return all current bindings

            SIP_Response response = m_pStack.CreateResponse(SIP_ResponseCodes.x200_Ok,request);
            response.Date = DateTime.Now;
            SIP_Registration registration = m_pRegistrations[to.Address];
            if(registration != null){
                foreach(SIP_RegistrationBinding b in registration.Bindings){
                    // Don't list expired bindings what wait to be disposed.
                    if(b.TTL > 1){
                        response.Header.Add("Contact:",b.ToContactValue());
                    }
                }
            }
            // Add Authentication-Info:, then client knows next nonce.
            response.AuthenticationInfo.Add("qop=\"auth\",nextnonce=\"" + m_pStack.DigestNonceManager.CreateNonce() + "\"");
            transaction.SendResponse(response);

            #endregion
                        
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets owner proxy core.
        /// </summary>
        public SIP_Proxy Proxy
        {
            get{ return m_pProxy; }
        }

        /// <summary>
        /// Gets current SIP registrations.
        /// </summary>
        public SIP_Registration[] Registrations
        {
            get{
                lock(m_pRegistrations){
                    SIP_Registration[] retVal = new SIP_Registration[m_pRegistrations.Count];
                    m_pRegistrations.Values.CopyTo(retVal,0);

                    return retVal;
                }
            }
        }

        #endregion

        #region Events Implementation

        /// <summary>
        /// This event is raised when SIP registrar need to check if specified user can register specified address.
        /// </summary>
        public event SIP_CanRegisterEventHandler CanRegister = null;

        #region method OnCanRegister

        /// <summary>
        /// Is called by SIP registrar if it needs to check if specified user can register specified address.
        /// </summary>
        /// <param name="userName">Authenticated user name.</param>
        /// <param name="address">Address to be registered.</param>
        /// <returns>Returns true if specified user can register specified address, otherwise false.</returns>
        internal bool OnCanRegister(string userName,string address)
        {
            if(this.CanRegister != null){
                return this.CanRegister(userName,address);
            }

            return false;
        }

        #endregion

        /// <summary>
        /// This event is raised when new AOR(address of record) has been registered.
        /// </summary>
        public event EventHandler<SIP_RegistrationEventArgs> AorRegistered = null;

        #region method OnAorRegistered

        /// <summary>
        /// Raises <b>AorRegistered</b> event.
        /// </summary>
        /// <param name="registration">SIP registration.</param>
        private void OnAorRegistered(SIP_Registration registration)
        {
            if(this.AorRegistered != null){
                this.AorRegistered(this,new SIP_RegistrationEventArgs(registration));
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when AOR(address of record) has been unregistered.
        /// </summary>
        public event EventHandler<SIP_RegistrationEventArgs> AorUnregistered = null;

        #region method OnAorUnregistered

        /// <summary>
        /// Raises <b>AorUnregistered</b> event.
        /// </summary>
        /// <param name="registration">SIP registration.</param>
        private void OnAorUnregistered(SIP_Registration registration)
        {
            if(this.AorUnregistered != null){
                this.AorUnregistered(this,new SIP_RegistrationEventArgs(registration));
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when AOR(address of record) has been updated.
        /// </summary>
        public event EventHandler<SIP_RegistrationEventArgs> AorUpdated = null;

        #region method OnAorUpdated

        /// <summary>
        /// Raises <b>AorUpdated</b> event.
        /// </summary>
        /// <param name="registration">SIP registration.</param>
        private void OnAorUpdated(SIP_Registration registration)
        {
            if(this.AorUpdated != null){
                this.AorUpdated(this,new SIP_RegistrationEventArgs(registration));
            }
        }

        #endregion

        #endregion

    }
}
