using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using LumiSoft.Net.SIP.Message;
using LumiSoft.Net.SIP.Stack;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class represent SIP UA registration.
    /// </summary>
    public class SIP_UA_Registration
    {
        private bool                     m_IsDisposed        = false;
        private SIP_UA_RegistrationState m_State             = SIP_UA_RegistrationState.Unregistered;
        private SIP_Stack                m_pStack            = null;
        private SIP_Uri                  m_pServer           = null;
        private string                   m_AOR               = "";
        private AbsoluteUri              m_pContact          = null;
        private List<AbsoluteUri>        m_pContacts         = null;
        private int                      m_RefreshInterval   = 300;
        private TimerEx                  m_pTimer            = null;
        private SIP_RequestSender        m_pRegisterSender   = null;
        private SIP_RequestSender        m_pUnregisterSender = null;
        private bool                     m_AutoRefresh       = true;
        private bool                     m_AutoDispose       = false;
        private SIP_Flow                 m_pFlow             = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stack">Owner SIP stack.</param>
        /// <param name="server">Registrar server URI. For example: sip:domain.com.</param>
        /// <param name="aor">Address of record. For example: user@domain.com.</param>
        /// <param name="contact">Contact URI.</param>
        /// <param name="expires">Gets after how many seconds reigisration expires.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ua</b>,<b>server</b>,<b>transport</b>,<b>aor</b> or <b>contact</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments contains invalid value.</exception>
        internal SIP_UA_Registration(SIP_Stack stack,SIP_Uri server,string aor,AbsoluteUri contact,int expires)
        {
            if(stack == null){
                throw new ArgumentNullException("stack");
            }
            if(server == null){
                throw new ArgumentNullException("server");
            }
            if(aor == null){
                throw new ArgumentNullException("aor");
            }
            if(aor == string.Empty){
                throw new ArgumentException("Argument 'aor' value must be specified.");
            }
            if(contact == null){
                throw new ArgumentNullException("contact");
            }

            m_pStack          = stack;
            m_pServer         = server;
            m_AOR             = aor;
            m_pContact        = contact;
            m_RefreshInterval = expires;

            m_pContacts = new List<AbsoluteUri>();

            m_pTimer = new TimerEx((m_RefreshInterval - 15) * 1000);
            m_pTimer.AutoReset = false;
            m_pTimer.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimer_Elapsed);
            m_pTimer.Enabled = false;
        }
                
        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
            if(m_IsDisposed){
                return;
            }
            m_IsDisposed = true;

            m_pStack = null;
            m_pTimer.Dispose();
            m_pTimer = null;
                     
            SetState(SIP_UA_RegistrationState.Disposed);
            OnDisposed();

            this.Registered   = null;
            this.Unregistered = null;
            this.Error        = null;
            this.Disposed     = null;
        }

        #endregion


        #region Events handling

        #region method m_pTimer_Elapsed

        /// <summary>
        /// This method is raised when registration needs to refresh server registration.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimer_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {   
            if(m_pStack.State == SIP_StackState.Started){
                BeginRegister(m_AutoRefresh);
            }
        }
                
        #endregion

        #region mehtod m_pRegisterSender_ResponseReceived

        /// <summary>
        /// This method is called when REGISTER has finished.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pRegisterSender_ResponseReceived(object sender,SIP_ResponseReceivedEventArgs e)
        {   
            m_pFlow = e.ClientTransaction.Flow;
       
            if(e.Response.StatusCodeType == SIP_StatusCodeType.Provisional){
                return;
            }
            else if(e.Response.StatusCodeType == SIP_StatusCodeType.Success){
                m_pContacts.Clear();
                foreach(SIP_t_ContactParam c in e.Response.Contact.GetAllValues()){
                    m_pContacts.Add(c.Address.Uri);
                }

                SetState(SIP_UA_RegistrationState.Registered);
                                             
                OnRegistered();

                m_pFlow.SendKeepAlives = true;
            }
            else{
                SetState(SIP_UA_RegistrationState.Error);

                OnError(e);
            }

            // REMOVE ME:
            if(this.AutoFixContact && (m_pContact is SIP_Uri)){
                // If Via: received or rport paramter won't match to our sent-by, use received and rport to construct new contact value.
                   
                SIP_Uri       cContact    = ((SIP_Uri)m_pContact);
                IPAddress     cContactIP  = Net_Utils.IsIPAddress(cContact.Host) ? IPAddress.Parse(cContact.Host) : null;
                SIP_t_ViaParm via         = e.Response.Via.GetTopMostValue();
                if(via != null && cContactIP != null){
                    IPEndPoint ep = new IPEndPoint(via.Received != null ? via.Received : cContactIP,via.RPort > 0 ? via.RPort : cContact.Port);
                    if(!cContactIP.Equals(ep.Address) || cContact.Port != via.RPort){
                        // Unregister old contact.
                        BeginUnregister(false);

                        // Fix contact.
                        cContact.Host = ep.Address.ToString();
                        cContact.Port = ep.Port;

                        m_pRegisterSender.Dispose();
                        m_pRegisterSender = null;
                                                
                        BeginRegister(m_AutoRefresh);

                        return;
                    }
                }
            }

            if(m_AutoRefresh){
                // Set registration refresh timer.
                m_pTimer.Enabled = true;
            }

            m_pRegisterSender.Dispose();
            m_pRegisterSender = null;
        }

        #endregion

        #region method m_pUnregisterSender_ResponseReceived

        /// <summary>
        /// This method is called when un-REGISTER has finished.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pUnregisterSender_ResponseReceived(object sender,SIP_ResponseReceivedEventArgs e)
        {
            SetState(SIP_UA_RegistrationState.Unregistered);
            OnUnregistered();

            if(m_AutoDispose){
                Dispose();
            }

            m_pUnregisterSender = null;
        }

        #endregion

        #endregion


        #region method BeginRegister

        /// <summary>
        /// Starts registering.
        /// </summary>
        /// <param name="autoRefresh">If true, registration takes care of refreshing itself to registrar server.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        public void BeginRegister(bool autoRefresh)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }

            // Fix ME: Stack not running, try register on next step.
            // In ideal solution we need to start registering when stack starts.
            if(m_pStack.State != SIP_StackState.Started){
                m_pTimer.Enabled = true;
                return;
            }

            m_AutoRefresh = autoRefresh;
            SetState(SIP_UA_RegistrationState.Registering);

            /* RFC 3261 10.1 Constructing the REGISTER Request.
                Request-URI: The Request-URI names the domain of the location service for which the registration is meant (for example,
                             "sip:chicago.com").  The "userinfo" and "@" components of the SIP URI MUST NOT be present.
            */

            SIP_Request register = m_pStack.CreateRequest(SIP_Methods.REGISTER,new SIP_t_NameAddress(m_pServer.Scheme + ":" + m_AOR),new SIP_t_NameAddress(m_pServer.Scheme + ":" + m_AOR));
            register.RequestLine.Uri = SIP_Uri.Parse(m_pServer.Scheme + ":" + m_AOR.Substring(m_AOR.IndexOf('@') + 1));
            register.Route.Add(m_pServer.ToString());
            register.Contact.Add("<" + this.Contact + ">;expires=" + m_RefreshInterval);

            m_pRegisterSender = m_pStack.CreateRequestSender(register,m_pFlow);
            m_pRegisterSender.ResponseReceived += new EventHandler<SIP_ResponseReceivedEventArgs>(m_pRegisterSender_ResponseReceived);
            m_pRegisterSender.Start();
        }

        #endregion

        #region method BeginUnregister

        /// <summary>
        /// Starts unregistering.
        /// </summary>
        /// <param name="dispose">If true, registration will be disposed after unregister.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        public void BeginUnregister(bool dispose)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }

            m_AutoDispose = dispose;

            // Stop register timer, otherwise we may get register and unregister race condition.
            m_pTimer.Enabled = false;

            if(m_State == SIP_UA_RegistrationState.Registered){
                /* RFC 3261 10.1 Constructing the REGISTER Request.
                    Request-URI: The Request-URI names the domain of the location service for which the registration is meant (for example,
                                 "sip:chicago.com").  The "userinfo" and "@" components of the SIP URI MUST NOT be present.
                */

                SIP_Request unregister = m_pStack.CreateRequest(SIP_Methods.REGISTER,new SIP_t_NameAddress(m_pServer.Scheme + ":" + m_AOR),new SIP_t_NameAddress(m_pServer.Scheme + ":" + m_AOR));
                unregister.RequestLine.Uri = SIP_Uri.Parse(m_pServer.Scheme + ":" + m_AOR.Substring(m_AOR.IndexOf('@') + 1));
                unregister.Route.Add(m_pServer.ToString());
                unregister.Contact.Add("<" + this.Contact + ">;expires=0");
            
                m_pUnregisterSender = m_pStack.CreateRequestSender(unregister,m_pFlow);
                m_pUnregisterSender.ResponseReceived += new EventHandler<SIP_ResponseReceivedEventArgs>(m_pUnregisterSender_ResponseReceived);
                m_pUnregisterSender.Start();
            }
            else{
                SetState(SIP_UA_RegistrationState.Unregistered);
                OnUnregistered();

                if(m_AutoDispose){
                    Dispose();
                }

                m_pUnregisterSender = null;
            }
        }
                
        #endregion


        #region method SetState

        /// <summary>
        /// Changes current registration state.
        /// </summary>
        /// <param name="newState">New registration state.</param>
        private void SetState(SIP_UA_RegistrationState newState)
        {
            m_State = newState;

            OnStateChanged();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if this object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get{ return m_IsDisposed; }
        }

        /// <summary>
        /// Gets registration state.
        /// </summary>
        public SIP_UA_RegistrationState State
        {
            get{ return m_State; }
        }

        /// <summary>
        /// Gets after how many seconds contact expires.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public int Expires
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return 3600; 
            }
        }

        /// <summary>
        /// Gets registration address of record.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public string AOR
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_AOR; 
            }
        }

        /// <summary>
        /// Gets registration contact URI.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public AbsoluteUri Contact
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pContact; 
            }
        }

        /// <summary>
        /// Gets registrar server all contacts registered for this AOR.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public AbsoluteUri[] Contacts
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pContacts.ToArray(); 
            }
        }
        
        /// <summary>
        /// If true and contact is different than received or rport, received and rport is used as contact.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public bool AutoFixContact
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                // TODO:

                return false; 
            }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// This event is raised when registration state has changed.
        /// </summary>
        public event EventHandler StateChanged = null;

        #region method OnStateChanged

        /// <summary>
        /// Raises event <b>StateChanged</b>.
        /// </summary>
        private void OnStateChanged()
        {
            if(this.StateChanged != null){
                this.StateChanged(this,new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when REGISTER has completed successfully.
        /// </summary>
        public event EventHandler Registered = null;

        #region method OnRegistered

        /// <summary>
        /// Raises event <b>Registered</b>.
        /// </summary>
        private void OnRegistered()
        {
            if(this.Registered != null){
                this.Registered(this,new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when un-REGISTER has completed successfully.
        /// </summary>
        public event EventHandler Unregistered = null;

        #region method OnUnregistered

        /// <summary>
        /// Raises event <b>Unregistered</b>.
        /// </summary>
        private void OnUnregistered()
        {
            if(this.Unregistered != null){
                this.Unregistered(this,new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when REGISTER/un-REGISTER has failed.
        /// </summary>
        public event EventHandler<SIP_ResponseReceivedEventArgs> Error = null;

        #region method OnError

        /// <summary>
        /// Raises event <b>Error</b>.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnError(SIP_ResponseReceivedEventArgs e)
        {
            if(this.Error != null){
                this.Error(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when registration has disposed.
        /// </summary>
        public event EventHandler Disposed = null;

        #region method OnDisposed

        /// <summary>
        /// Raises event <b>Disposed</b>.
        /// </summary>
        private void OnDisposed()
        {
            if(this.Disposed != null){
                this.Disposed(this,new EventArgs());
            }
        }

        #endregion

        #endregion

    }
}
