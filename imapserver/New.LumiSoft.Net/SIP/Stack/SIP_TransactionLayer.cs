using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Timers;

using LumiSoft.Net.SIP.Message;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// Implements SIP transaction layer. Defined in RFC 3261.
    /// Transaction layer manages client,server transactions and dialogs.
    /// </summary>
    public class SIP_TransactionLayer
    {        
        private bool                                     m_IsDisposed          = false;
        private SIP_Stack                                m_pStack              = null;
        private Dictionary<string,SIP_ClientTransaction> m_pClientTransactions = null;
        private Dictionary<string,SIP_ServerTransaction> m_pServerTransactions = null;
        private Dictionary<string,SIP_Dialog>            m_pDialogs            = null;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="stack">Reference to SIP stack.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stack</b> is null reference.</exception>
        internal SIP_TransactionLayer(SIP_Stack stack)
        {
            if(stack == null){
                throw new ArgumentNullException("stack");
            }

            m_pStack = stack;

            m_pClientTransactions = new Dictionary<string,SIP_ClientTransaction>();
            m_pServerTransactions = new Dictionary<string,SIP_ServerTransaction>();
            m_pDialogs            = new Dictionary<string,SIP_Dialog>();
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

            foreach(SIP_ClientTransaction tr in this.ClientTransactions){
                try{
                    tr.Dispose();
                }
                catch{
                }
            }
            foreach(SIP_ServerTransaction tr in this.ServerTransactions){
                try{
                    tr.Dispose();
                }
                catch{
                }
            }
            foreach(SIP_Dialog dialog in this.Dialogs){
                try{
                    dialog.Dispose();
                }
                catch{
                }
            }

            m_IsDisposed = true;
        }

        #endregion


        #region Events Handling

        #endregion


        #region method CreateClientTransaction

        /// <summary>
        /// Creates new client transaction.
        /// </summary>
        /// <param name="flow">SIP data flow which is used to send request.</param>
        /// <param name="request">SIP request that transaction will handle.</param>
        /// <param name="addVia">If true, transaction will add <b>Via:</b> header, otherwise it's user responsibility.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>flow</b> or <b>request</b> is null reference.</exception>
        /// <returns>Returns created transaction.</returns>
        public SIP_ClientTransaction CreateClientTransaction(SIP_Flow flow,SIP_Request request,bool addVia)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(flow == null){
                throw new ArgumentNullException("flow");
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }

            // Add Via:
            if(addVia){
                SIP_t_ViaParm via = new SIP_t_ViaParm();
                via.ProtocolName = "SIP";
                via.ProtocolVersion = "2.0";
                via.ProtocolTransport = flow.Transport;
                via.SentBy = new HostEndPoint("transport_layer_will_replace_it",-1);
                via.Branch = SIP_t_ViaParm.CreateBranch();
                via.RPort = 0;
                request.Via.AddToTop(via.ToStringValue());
            }
                        
            lock(m_pClientTransactions){
                SIP_ClientTransaction transaction = new SIP_ClientTransaction(m_pStack,flow,request);              
                m_pClientTransactions.Add(transaction.Key,transaction);
                transaction.StateChanged += new EventHandler(delegate(object s,EventArgs e){
                    if(transaction.State == SIP_TransactionState.Terminated){
                        lock(m_pClientTransactions){
                            m_pClientTransactions.Remove(transaction.Key);
                        }
                    }
                });

                SIP_Dialog dialog = MatchDialog(request);
                if(dialog != null){
                    dialog.AddTransaction(transaction);
                }

                return transaction;
            }
        }

        #endregion

        #region method CreateServerTransaction

        /// <summary>
        /// Creates new SIP server transaction for specified request.
        /// </summary>
        /// <param name="flow">SIP data flow which is used to receive request.</param>
        /// <param name="request">SIP request.</param>
        /// <returns>Returns added server transaction.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>flow</b> or <b>request</b> is null reference.</exception>
        public SIP_ServerTransaction CreateServerTransaction(SIP_Flow flow,SIP_Request request)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(flow == null){
                throw new ArgumentNullException("flow");
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }

            lock(m_pServerTransactions){
                SIP_ServerTransaction transaction = new SIP_ServerTransaction(m_pStack,flow,request);
                m_pServerTransactions.Add(transaction.Key,transaction);
                transaction.StateChanged += new EventHandler(delegate(object s,EventArgs e){
                    if(transaction.State == SIP_TransactionState.Terminated){
                        lock(m_pClientTransactions){
                            m_pServerTransactions.Remove(transaction.Key);
                        }
                    }
                });

                SIP_Dialog dialog = MatchDialog(request);
                if(dialog != null){
                    dialog.AddTransaction(transaction);
                }

                return transaction;
            }
        }

        #endregion

        #region method EnsureServerTransaction

        /// <summary>
        /// Ensures that specified request has matching server transaction. If server transaction doesn't exist, 
        /// it will be created, otherwise existing transaction will be returned.
        /// </summary>
        /// <param name="flow">SIP data flow which is used to receive request.</param>
        /// <param name="request">SIP request.</param>
        /// <returns>Returns matching transaction.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>flow</b> or <b>request</b> is null.</exception>
        /// <exception cref="InvalidOperationException">Is raised when request.Method is ACK request.</exception>
        public SIP_ServerTransaction EnsureServerTransaction(SIP_Flow flow,SIP_Request request)
        {
            if(flow == null){
                throw new ArgumentNullException("flow");
            }
            if(request == null){
                throw new ArgumentNullException("request");
            }
            if(request.RequestLine.Method == SIP_Methods.ACK){
                throw new InvalidOperationException("ACK request is transaction less request, can't create transaction for it.");
            }

            /*
                We use branch and sent-by as indexing key for transaction, the only special what we need to 
                do is to handle CANCEL, because it has same branch as transaction to be canceled.
                For avoiding key collision, we add branch + '-' + 'sent-by' + CANCEL for cancel index key.
                ACK has also same branch, but we won't do transaction for ACK, so it isn't problem.
            */
            string key = request.Via.GetTopMostValue().Branch + '-' + request.Via.GetTopMostValue().SentBy;
            if(request.RequestLine.Method == SIP_Methods.CANCEL){
                key += "-CANCEL";
            }

            lock(m_pServerTransactions){
                SIP_ServerTransaction retVal = null;
                m_pServerTransactions.TryGetValue(key,out retVal);
                // We don't have transaction, create it.
                if(retVal == null){
                    retVal = CreateServerTransaction(flow,request);
                }

                return retVal;
            }
        }

        #endregion


        #region method MatchClientTransaction

        /// <summary>
        /// Matches SIP response to client transaction. If not matching transaction found, returns null.
        /// </summary>
        /// <param name="response">SIP response to match.</param>
        internal SIP_ClientTransaction MatchClientTransaction(SIP_Response response)
        {
            /* RFC 3261 17.1.3 Matching Responses to Client Transactions.
                1.  If the response has the same value of the branch parameter in
                    the top Via header field as the branch parameter in the top
                    Via header field of the request that created the transaction.

                2.  If the method parameter in the CSeq header field matches the
                    method of the request that created the transaction.  The
                    method is needed since a CANCEL request constitutes a
                    different transaction, but shares the same value of the branch
                    parameter.
            */

            SIP_ClientTransaction retVal = null;

            string transactionID = response.Via.GetTopMostValue().Branch + "-" + response.CSeq.RequestMethod;
            lock(m_pClientTransactions){
                m_pClientTransactions.TryGetValue(transactionID,out retVal);
            }
            
            return retVal;
        }

        #endregion

        #region method MatchServerTransaction

        /// <summary>
        /// Matches SIP request to server transaction. If not matching transaction found, returns null.
        /// </summary>
        /// <param name="request">SIP request to match.</param>
        /// <returns>Returns matching transaction or null if no match.</returns>
        internal SIP_ServerTransaction MatchServerTransaction(SIP_Request request)
        {
            /* RFC 3261 17.2.3 Matching Requests to Server Transactions.
                This matching rule applies to both INVITE and non-INVITE transactions.

                1. the branch parameter in the request is equal to the one in the top Via header 
                   field of the request that created the transaction, and

                2. the sent-by value in the top Via of the request is equal to the
                   one in the request that created the transaction, and

                3. the method of the request matches the one that created the transaction, except 
                   for ACK, where the method of the request that created the transaction is INVITE.
             
                Internal implementation notes:
                    Inernally we use branch + '-' + sent-by for non-CANCEL and for CANCEL
                    branch + '-' + sent-by + '-' CANCEL. This is because method matching is actually
                    needed for CANCEL only (CANCEL shares cancelable transaction branch ID).
            */

            SIP_ServerTransaction retVal = null;

            /*
                We use branch and sent-by as indexing key for transaction, the only special what we need to 
                do is to handle CANCEL, because it has same branch as transaction to be canceled.
                For avoiding key collision, we add branch + '-' + 'sent-by' + CANCEL for cancel index key.
                ACK has also same branch, but we won't do transaction for ACK, so it isn't problem.
            */
            string key = request.Via.GetTopMostValue().Branch + '-' + request.Via.GetTopMostValue().SentBy;
            if(request.RequestLine.Method == SIP_Methods.CANCEL){
                key += "-CANCEL";
            }

            lock(m_pServerTransactions){
                m_pServerTransactions.TryGetValue(key,out retVal);
            }

            // Don't match ACK for terminated transaction, in that case ACK must be passed to "core".
            if(retVal != null && request.RequestLine.Method == SIP_Methods.ACK && retVal.State == SIP_TransactionState.Terminated){
                retVal = null;
            }

            return retVal;
        }

        #endregion

        #region method MatchCancelToTransaction

        /// <summary>
        /// Matches CANCEL requst to SIP server non-CANCEL transaction. Returns null if no match.
        /// </summary>
        /// <param name="cancelRequest">SIP CANCEL request.</param>
        /// <returns>Returns CANCEL matching server transaction or null if no match.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>cancelTransaction</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when <b>cancelTransaction</b> has invalid.</exception>
        public SIP_ServerTransaction MatchCancelToTransaction(SIP_Request cancelRequest)
        {
            if(cancelRequest == null){
                throw new ArgumentNullException("cancelRequest");
            }
            if(cancelRequest.RequestLine.Method != SIP_Methods.CANCEL){
                throw new ArgumentException("Argument 'cancelRequest' is not SIP CANCEL request.");
            }

            SIP_ServerTransaction retVal = null;

            // NOTE: There we don't add '-CANCEL' because we want to get CANCEL matching transaction, not CANCEL
            //       transaction itself.
            string key = cancelRequest.Via.GetTopMostValue().Branch + '-' + cancelRequest.Via.GetTopMostValue().SentBy;            
            lock(m_pServerTransactions){
                m_pServerTransactions.TryGetValue(key,out retVal);
            }

            return retVal;
        }

        #endregion


        #region method GetOrCreateDialog

        /// <summary>
        /// Gets existing or creates new dialog.
        /// </summary>
        /// <param name="transaction">Owner transaction what forces to create dialog.</param>
        /// <param name="response">Response what forces to create dialog.</param>
        /// <returns>Returns dialog.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>transaction</b> or <b>response</b> is null.</exception>
        public SIP_Dialog GetOrCreateDialog(SIP_Transaction transaction,SIP_Response response)
        {
            if(transaction == null){
                throw new ArgumentNullException("transaction");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            string dialogID = "";
            if(transaction is SIP_ServerTransaction){
                dialogID = response.CallID + "-" + response.To.Tag + "-" + response.From.Tag;
            }
            else{
                dialogID = response.CallID + "-" + response.From.Tag + "-" + response.To.Tag; 
            }

            lock(m_pDialogs){ 
                SIP_Dialog dialog = null;
                m_pDialogs.TryGetValue(dialogID,out dialog);
                // Dialog doesn't exist, create it.
                if(dialog == null){
                    if(response.CSeq.RequestMethod.ToUpper() == SIP_Methods.INVITE){
                        dialog = new SIP_Dialog_Invite();                        
                    }
                    else if(response.CSeq.RequestMethod.ToUpper() == SIP_Methods.REFER){
                        dialog = new SIP_Dialog_Refer();
                    }
                    else{
                        throw new ArgumentException("Method '" + response.CSeq.RequestMethod + "' has no dialog handler.");
                    }

                    dialog.Init(m_pStack,transaction,response);
                    dialog.StateChanged += delegate(object s,EventArgs a){
                        if(dialog.State == SIP_DialogState.Terminated){
                            m_pDialogs.Remove(dialog.ID);
                        }
                    };
                    m_pDialogs.Add(dialog.ID,dialog);
                }

                return dialog;
            }
        }

        #endregion


        #region method RemoveDialog

        /// <summary>
        /// Removes specified dialog from dialogs collection.
        /// </summary>
        /// <param name="dialog">SIP dialog to remove.</param>
        internal void RemoveDialog(SIP_Dialog dialog)
        {
            lock(m_pDialogs){
                m_pDialogs.Remove(dialog.ID);
            }
        }

        #endregion

        #region method MatchDialog

        /// <summary>
        /// Matches specified SIP request to SIP dialog. If no matching dialog found, returns null.
        /// </summary>
        /// <param name="request">SIP request.</param>
        /// <returns>Returns matched SIP dialog or null in no match found.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>request</b> is null.</exception>
        internal SIP_Dialog MatchDialog(SIP_Request request)
        {
            if(request == null){
                throw new ArgumentNullException("request");
            }

            SIP_Dialog dialog = null;

            try{
                string callID    = request.CallID;
                string localTag  = request.To.Tag;
                string remoteTag = request.From.Tag;                                        
                if(callID != null && localTag != null && remoteTag != null){
                    string dialogID = callID + "-" + localTag + "-" + remoteTag;
                    lock(m_pDialogs){                        
                        m_pDialogs.TryGetValue(dialogID,out dialog);
                    }
                }
            }
            catch{
            }

            return dialog;
        }

        /// <summary>
        /// Matches specified SIP response to SIP dialog. If no matching dialog found, returns null.
        /// </summary>
        /// <param name="response">SIP response.</param>
        /// <returns>Returns matched SIP dialog or null in no match found.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null.</exception>
        internal SIP_Dialog MatchDialog(SIP_Response response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            SIP_Dialog dialog = null;

            try{
                string callID  = response.CallID;
                string fromTag = response.From.Tag; 
                string toTag   = response.To.Tag;                                       
                if(callID != null && fromTag != null && toTag != null){
                    string dialogID = callID + "-" + fromTag + "-" + toTag;
                    lock(m_pDialogs){
                        m_pDialogs.TryGetValue(dialogID,out dialog);
                    }
                }
            }
            catch{
            }

            return dialog;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets all(clinet + server) active transactions.
        /// </summary>
        public SIP_Transaction[] Transactions
        {
            get{
                List<SIP_Transaction> retVal = new List<SIP_Transaction>();
                retVal.AddRange(this.ClientTransactions);
                retVal.AddRange(this.ServerTransactions);

                return retVal.ToArray();
            }
        }

        /// <summary>
        /// Gets all available client transactions. This method is thread-safe.
        /// </summary>
        public SIP_ClientTransaction[] ClientTransactions
        {
            get{ 
                lock(m_pClientTransactions){
                    SIP_ClientTransaction[] retVal = new SIP_ClientTransaction[m_pClientTransactions.Values.Count];
                    m_pClientTransactions.Values.CopyTo(retVal,0);

                    return retVal; 
                }
            }
        }

        /// <summary>
        /// Gets all available server transactions. This method is thread-safe.
        /// </summary>
        public SIP_ServerTransaction[] ServerTransactions
        {
            get{ 
                lock(m_pServerTransactions){
                    SIP_ServerTransaction[] retVal = new SIP_ServerTransaction[m_pServerTransactions.Values.Count];
                    m_pServerTransactions.Values.CopyTo(retVal,0);

                    return retVal; 
                }
            }
        }

        /// <summary>
        /// Gets active dialogs. This method is thread-safe.
        /// </summary>
        public SIP_Dialog[] Dialogs
        {
            get{ 
                lock(m_pDialogs){
                    SIP_Dialog[] retVal = new SIP_Dialog[m_pDialogs.Values.Count];
                    m_pDialogs.Values.CopyTo(retVal,0);

                    return retVal; 
                }
            }
        }

        #endregion

    }
}
