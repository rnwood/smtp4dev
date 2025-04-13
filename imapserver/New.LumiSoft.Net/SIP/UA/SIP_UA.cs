using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using LumiSoft.Net.Media.Codec.Audio;
using LumiSoft.Net.SIP;
using LumiSoft.Net.SIP.Stack;
using LumiSoft.Net.SIP.Message;

namespace LumiSoft.Net.SIP.UA
{
    /// <summary>
    /// This class implements SIP UA. Defined in RFC 3261 8.1.
    /// </summary>
    [Obsolete("Use SIP stack instead.")]
    public class SIP_UA : IDisposable
    {
        private bool              m_IsDisposed = false;
        private SIP_Stack         m_pStack     = null;
        private List<SIP_UA_Call> m_pCalls     = null;
        private object            m_pLock      = new object();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_UA()
        {
            m_pStack = new SIP_Stack();
            m_pStack.RequestReceived += new EventHandler<SIP_RequestReceivedEventArgs>(m_pStack_RequestReceived);

            m_pCalls = new List<SIP_UA_Call>();
        }
                        
        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
            lock(m_pLock){
                if(m_IsDisposed){
                    return;
                }
                                            
                // Hang up all calls.
                foreach(SIP_UA_Call call in m_pCalls.ToArray()){
                    call.Terminate();
                }

                // Wait till all registrations and calls disposed or wait timeout reached.
                DateTime start = DateTime.Now;
                while(m_pCalls.Count > 0){
                    System.Threading.Thread.Sleep(500);

                    // Timeout, just kill all UA.
                    if(((TimeSpan)(DateTime.Now - start)).Seconds > 15){
                        break;
                    }
                }

                m_IsDisposed = true;

                this.RequestReceived = null;
                this.IncomingCall = null;

                m_pStack.Dispose();
                m_pStack = null;               
            }
        }

        #endregion


        #region Events handling

        #region method m_pStack_RequestReceived

        /// <summary>
        /// This method is called when SIP stack received new message.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pStack_RequestReceived(object sender,SIP_RequestReceivedEventArgs e)
        {
            // TODO: Performance: rise events on thread pool or see if this method called on pool aready, then we may not keep lock for events ?

            if(e.Request.RequestLine.Method == SIP_Methods.CANCEL){
                /* RFC 3261 9.2.
                    If the UAS did not find a matching transaction for the CANCEL
                    according to the procedure above, it SHOULD respond to the CANCEL
                    with a 481 (Call Leg/Transaction Does Not Exist).
                  
                    Regardless of the method of the original request, as long as the
                    CANCEL matched an existing transaction, the UAS answers the CANCEL
                    request itself with a 200 (OK) response.
                */

                SIP_ServerTransaction trToCancel = m_pStack.TransactionLayer.MatchCancelToTransaction(e.Request);
                if(trToCancel != null){
                    trToCancel.Cancel();
                    e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x200_Ok,e.Request));
                }
                else{
                    e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x481_Call_Transaction_Does_Not_Exist,e.Request));
                }
            }
            else if(e.Request.RequestLine.Method == SIP_Methods.BYE){
                /* RFC 3261 15.1.2.
                    If the BYE does not match an existing dialog, the UAS core SHOULD generate a 481
                    (Call/Transaction Does Not Exist) response and pass that to the server transaction.
                */
                // TODO:

                SIP_Dialog dialog = m_pStack.TransactionLayer.MatchDialog(e.Request);
                if(dialog != null){
                    e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x200_Ok,e.Request));
                    dialog.Terminate();
                }
                else{
                    e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x481_Call_Transaction_Does_Not_Exist,e.Request));
                }
            }
            else if(e.Request.RequestLine.Method == SIP_Methods.INVITE){
                // Supress INVITE retransmissions.
                e.ServerTransaction.SendResponse(m_pStack.CreateResponse(SIP_ResponseCodes.x100_Trying,e.Request));
         
                // Create call.
                SIP_UA_Call call = new SIP_UA_Call(this,e.ServerTransaction);
                call.StateChanged += new EventHandler(Call_StateChanged);
                m_pCalls.Add(call);

                OnIncomingCall(call);
            }
            else{
                OnRequestReceived(e);
            }
        }

        #endregion

        #region method Call_StateChanged

        /// <summary>
        /// Thsi method is called when call state has chnaged.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void Call_StateChanged(object sender,EventArgs e)
        {
            SIP_UA_Call call = (SIP_UA_Call)sender;            
            if(call.State == SIP_UA_CallState.Terminated){
                m_pCalls.Remove(call);
            }
        }

        #endregion

        #endregion


        #region method CreateCall

        /// <summary>
        /// Creates call to <b>invite</b> specified recipient.
        /// </summary>
        /// <param name="invite">INVITE request.</param>
        /// <returns>Returns created call.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>invite</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the argumnets has invalid value.</exception>
        public SIP_UA_Call CreateCall(SIP_Request invite)
        {
            if(invite == null){
                throw new ArgumentNullException("invite");
            }
            if(invite.RequestLine.Method != SIP_Methods.INVITE){
                throw new ArgumentException("Argument 'invite' is not INVITE request.");
            }

            lock(m_pLock){
                SIP_UA_Call call = new SIP_UA_Call(this,invite);
                call.StateChanged += new EventHandler(Call_StateChanged);
                m_pCalls.Add(call);

                return call;
            }
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
        /// Gets SIP stack.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_Stack Stack
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pStack; 
            }
        }

        /// <summary>
        /// Gets active calls.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
        public SIP_UA_Call[] Calls
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pCalls.ToArray();
            }
        }
                
        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when user agent get new SIP request.
        /// </summary>
        public event EventHandler<SIP_RequestReceivedEventArgs> RequestReceived = null;

        #region method OnRequestReceived

        /// <summary>
        /// Raises <b>RequestReceived</b> event.
        /// </summary>
        /// <param name="request">SIP request.</param>
        protected void OnRequestReceived(SIP_RequestReceivedEventArgs request)
        {
            if(this.RequestReceived != null){
                this.RequestReceived(this,request);
            }
        }

        #endregion

        /// <summary>
        /// Is raised when new incoming call.
        /// </summary>
        public event EventHandler<SIP_UA_Call_EventArgs> IncomingCall = null;

        #region method OnIncomingCall

        /// <summary>
        /// Raises event <b>IncomingCall</b>.
        /// </summary>
        /// <param name="call">Incoming call.</param>
        private void OnIncomingCall(SIP_UA_Call call)
        {
            if(this.IncomingCall != null){
                this.IncomingCall(this,new SIP_UA_Call_EventArgs(call));
            }
        }

        #endregion

        #endregion

    }
}
