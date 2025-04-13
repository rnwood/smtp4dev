using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.SIP.Message;

using LumiSoft.Net.SDP;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class represents INVITE dialog. Defined in RFC 3261.
    /// </summary>
    public class SIP_Dialog_Invite : SIP_Dialog
    {           
        private SIP_Transaction m_pActiveInvite             = null;
        private bool            m_IsTerminatedByRemoteParty = false;
        private string          m_TerminateReason           = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal SIP_Dialog_Invite()
        {            
        }

        #region overide method Init

        /// <summary>
        /// Initializes dialog.
        /// </summary>
        /// <param name="stack">Owner stack.</param>
        /// <param name="transaction">Owner transaction.</param>
        /// <param name="response">SIP response what caused dialog creation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stack</b>,<b>transaction</b> or <b>response</b>.</exception>
        internal protected override void Init(SIP_Stack stack,SIP_Transaction transaction,SIP_Response response)
        {
            if(stack == null){
                throw new ArgumentNullException("stack");
            }
            if(transaction == null){
                throw new ArgumentNullException("transaction");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            base.Init(stack,transaction,response);

            if(transaction is SIP_ServerTransaction){
                if(response.StatusCodeType == SIP_StatusCodeType.Success){
                    SetState(SIP_DialogState.Early,false);                    
                }
                else if(response.StatusCodeType == SIP_StatusCodeType.Provisional){
                    SetState(SIP_DialogState.Early,false);
                                        
                    m_pActiveInvite = transaction;                    
                    m_pActiveInvite.StateChanged += delegate(object s,EventArgs a){
                        if(m_pActiveInvite != null && m_pActiveInvite.State == SIP_TransactionState.Terminated){
                            m_pActiveInvite = null;

                            /* RFC 3261 13.3.1.4.
                                If the server retransmits the 2xx response for 64*T1 seconds without
                                receiving an ACK, the dialog is confirmed, but the session SHOULD be
                                terminated. 
                            */
                            if(this.State == SIP_DialogState.Early){
                                this.SetState(SIP_DialogState.Confirmed,true);
                                Terminate("ACK was not received for initial INVITE 2xx response.",true);
                            }
                            else if(this.State == SIP_DialogState.Terminating){
                                this.SetState(SIP_DialogState.Confirmed,false);
                                Terminate(m_TerminateReason,true);
                            }
                        }
                    };
                }
                else{
                    throw new ArgumentException("Argument 'response' has invalid status code, 1xx - 2xx is only allowed.");
                }
            }
            else{
                if(response.StatusCodeType == SIP_StatusCodeType.Success){
                    SetState(SIP_DialogState.Confirmed,false);
                
                }
                else if(response.StatusCodeType == SIP_StatusCodeType.Provisional){
                    SetState(SIP_DialogState.Early,false);

                    m_pActiveInvite = transaction;                                      
                    m_pActiveInvite.StateChanged += delegate(object s,EventArgs a){
                        if(m_pActiveInvite != null && m_pActiveInvite.State == SIP_TransactionState.Terminated){
                            m_pActiveInvite = null;
                        }
                    };

                    // Once we receive 2xx response, dialog will switch to confirmed state.
                    ((SIP_ClientTransaction)transaction).ResponseReceived += delegate(object s,SIP_ResponseReceivedEventArgs a){
                        if(a.Response.StatusCodeType == SIP_StatusCodeType.Success){
                            
                            SetState(SIP_DialogState.Confirmed,true);
                        }
                    };
                }
                else{
                    throw new ArgumentException("Argument 'response' has invalid status code, 1xx - 2xx is only allowed.");
                }
            }
        }

        #endregion

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public override void Dispose()
        {
            lock(this.SyncRoot){
                if(this.State == SIP_DialogState.Disposed){
                    return;
                }
                                
                m_pActiveInvite = null;

                base.Dispose();
            }
        }

        #endregion


        #region method Terminate

        /// <summary>
        /// Starts terminating dialog.
        /// </summary>
        /// <param name="reason">Termination reason. This value may be null.</param>
        /// <param name="sendBye">If true BYE is sent to remote party.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        public void Terminate(string reason,bool sendBye)
        {
            lock(this.SyncRoot){
                if(this.State == SIP_DialogState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(this.State == SIP_DialogState.Terminating || this.State == SIP_DialogState.Terminated){
                    return;
                }

                m_TerminateReason = reason;

                /* RFC 3261 15.
                    The caller's UA MAY send a BYE for either confirmed or early dialogs, and the callee's UA MAY send a BYE on
                    confirmed dialogs, but MUST NOT send a BYE on early dialogs.
                 
                   RFC 3261 15.1.
                    Once the BYE is constructed, the UAC core creates a new non-INVITE client transaction, and passes it the BYE request.
                    The UAC MUST consider the session terminated (and therefore stop sending or listening for media) as soon as the BYE 
                    request is passed to the client transaction. If the response for the BYE is a 481 (Call/Transaction Does Not Exist) 
                    or a 408 (Request Timeout) or no response at all is received for the BYE (that is, a timeout is returned by the 
                    client transaction), the UAC MUST consider the session and the dialog terminated.
                */ 
                                
                if(sendBye){
                    if((this.State == SIP_DialogState.Early && m_pActiveInvite is SIP_ClientTransaction) || this.State == SIP_DialogState.Confirmed){
                        this.SetState(SIP_DialogState.Terminating,true);

                        SIP_Request bye = CreateRequest(SIP_Methods.BYE);
                        if(!string.IsNullOrEmpty(reason)){
                            SIP_t_ReasonValue r = new SIP_t_ReasonValue();
                            r.Protocol = "SIP";
                            r.Text = reason;
                            bye.Reason.Add(r.ToStringValue());
                        }

                        // Send BYE, just wait BYE to complete, we don't care about response code.
                        SIP_RequestSender sender = CreateRequestSender(bye);
                        sender.Completed += delegate(object s,EventArgs a){
                            this.SetState(SIP_DialogState.Terminated,true);
                        };
                        sender.Start();
                    }
                    else{
                        /* We are "early" UAS dialog, we need todo follwoing:
                            *) If we havent sent final response, send '408 Request terminated' and we are done.
                            *) We have sen't final response, we need to wait ACK to arrive or timeout.
                                If will ACK arrives or timeout, send BYE.                                
                        */

                        if(m_pActiveInvite != null && m_pActiveInvite.FinalResponse == null){
                            this.Stack.CreateResponse(SIP_ResponseCodes.x408_Request_Timeout,m_pActiveInvite.Request);

                            this.SetState(SIP_DialogState.Terminated,true);
                        }
                        else{ 
                            // Wait ACK to arrive or timeout. 

                            this.SetState(SIP_DialogState.Terminating,true);
                        }
                    }
                }
                else{
                    this.SetState(SIP_DialogState.Terminated,true);
                }
            }
        }

        #endregion


        #region method ProcessRequest

        /// <summary>
        /// Processes specified request through this dialog.
        /// </summary>
        /// <param name="e">Method arguments.</param>
        /// <returns>Returns true if this dialog processed specified request, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>e</b> is null reference.</exception>
        internal protected override bool ProcessRequest(SIP_RequestReceivedEventArgs e)
        {
            if(e == null){
                throw new ArgumentNullException("e");
            }

            if(base.ProcessRequest(e)){
                return true;
            }

            if(e.Request.RequestLine.Method == SIP_Methods.ACK){
                if(this.State == SIP_DialogState.Early){
                    this.SetState(SIP_DialogState.Confirmed,true);
                }
                else if(this.State == SIP_DialogState.Terminating){
                    this.SetState(SIP_DialogState.Confirmed,false);

                    Terminate(m_TerminateReason,true);
                }
            }
            else if(e.Request.RequestLine.Method == SIP_Methods.BYE){
                e.ServerTransaction.SendResponse(this.Stack.CreateResponse(SIP_ResponseCodes.x200_Ok,e.Request));

                m_IsTerminatedByRemoteParty = true;
                OnTerminatedByRemoteParty(e);
                SetState(SIP_DialogState.Terminated,true);   
             
                return true;
            }
            else if(e.Request.RequestLine.Method == SIP_Methods.INVITE){
                /* RFC 3261 14.2.
                    A UAS that receives a second INVITE before it sends the final
                    response to a first INVITE with a lower CSeq sequence number on the
                    same dialog MUST return a 500 (Server Internal Error) response to the
                    second INVITE and MUST include a Retry-After header field with a
                    randomly chosen value of between 0 and 10 seconds.
                */
                // Dialog base class will handle this case.
                /*
                foreach(SIP_Transaction tr in this.Transactions){
                    if(tr is SIP_ServerTransaction && (tr.State == SIP_TransactionState.Calling || tr.State == SIP_TransactionState.Proceeding)){
                        if(e.Request.CSeq.SequenceNumber < tr.Request.CSeq.SequenceNumber){
                            SIP_Response response = this.Stack.CreateResponse(SIP_ResponseCodes.x500_Server_Internal_Error + ": INVITE with higher Seq sequence number is progress.",e.Request);
                            response.RetryAfter = new SIP_t_RetryAfter((new Random()).Next(1,10).ToString());
                            e.ServerTransaction.SendResponse(response);

                            return true;
                        }
         
                        break;
                    }
                }*/

                /* RFC 3261 14.2.
                    A UAS that receives an INVITE on a dialog while an INVITE it had sent
                    on that dialog is in progress MUST return a 491 (Request Pending)
                    response to the received INVITE.
                */
                if(this.HasPendingInvite){
                    e.ServerTransaction.SendResponse(this.Stack.CreateResponse(SIP_ResponseCodes.x491_Request_Pending,e.Request));

                    return true;
                }
            }
            // RFC 5057 5.6. Refusing New Usages. Decline(603 Decline) new dialog usages.
            else if(SIP_Utils.MethodCanEstablishDialog(e.Request.RequestLine.Method)){
                e.ServerTransaction.SendResponse(this.Stack.CreateResponse(SIP_ResponseCodes.x603_Decline + " : New dialog usages in dialog not allowed (RFC 5057).",e.Request));

                return true;
            }

            return false;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if dialog has pending INVITE transaction.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public bool HasPendingInvite
        {
            get{
                if(this.State == SIP_DialogState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                foreach(SIP_Transaction tr in this.Transactions){
                    if(tr.State == SIP_TransactionState.Calling || tr.State == SIP_TransactionState.Proceeding){
                        return true;
                    }
                }

                return false;                
            }
        }

        /// <summary>
        /// Gets if dialog was terminated by remote-party.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public bool IsTerminatedByRemoteParty
        {
            get{                
                if(this.State == SIP_DialogState.Disposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_IsTerminatedByRemoteParty; 
            }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// This event is raised when remote-party terminates dialog with BYE request.
        /// </summary>
        /// <remarks>This event is useful only if the application is interested in processing the headers in the BYE message.</remarks>
        public event EventHandler<SIP_RequestReceivedEventArgs> TerminatedByRemoteParty = null;

        #region method OnTerminatedByRemoteParty

        /// <summary>
        /// Raises <b>TerminatedByRemoteParty</b> event.
        /// </summary>
        /// <param name="bye">BYE request.</param>
        private void OnTerminatedByRemoteParty(SIP_RequestReceivedEventArgs bye)
        {
            if(this.TerminatedByRemoteParty != null){
                this.TerminatedByRemoteParty(this,bye);
            }
        }

        #endregion

        #endregion

    }
}
