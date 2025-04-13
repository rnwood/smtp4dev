using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.SIP.Message;
using LumiSoft.Net.SIP.Stack;
using LumiSoft.Net.AUTH;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// This class represents B2BUA call.
    /// </summary>
    public class SIP_B2BUA_Call
    {        
        private SIP_B2BUA  m_pOwner       = null;
        private DateTime   m_StartTime;
        private SIP_Dialog m_pCaller      = null;
        private SIP_Dialog m_pCallee      = null;
        private string     m_CallID       = "";
        private bool       m_IsTerminated = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="owner">Owner B2BUA server.</param>
        /// <param name="caller">Caller side dialog.</param>
        /// <param name="callee">Callee side dialog.</param>
        internal SIP_B2BUA_Call(SIP_B2BUA owner,SIP_Dialog caller,SIP_Dialog callee)
        {
            m_pOwner    = owner;
            m_pCaller   = caller;
            m_pCallee   = callee;
            m_StartTime = DateTime.Now;
            m_CallID    = Guid.NewGuid().ToString().Replace("-","");

            //m_pCaller.RequestReceived += new SIP_RequestReceivedEventHandler(m_pCaller_RequestReceived);
            //m_pCaller.Terminated += new EventHandler(m_pCaller_Terminated);

            //m_pCallee.RequestReceived += new SIP_RequestReceivedEventHandler(m_pCallee_RequestReceived);
            //m_pCallee.Terminated += new EventHandler(m_pCallee_Terminated);           
        }
                                            

        #region Events Handling

        #region method m_pCaller_RequestReceived

        /// <summary>
        /// Is called when caller sends new request.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void m_pCaller_RequestReceived(SIP_RequestReceivedEventArgs e)
        {  
            // TODO: If we get UPDATE, but callee won't support it ? generate INVITE instead ?
    /*
            SIP_Request request = m_pCallee.CreateRequest(e.Request.RequestLine.Method);
            CopyMessage(e.Request,request,new string[]{"Via:","Call-Id:","To:","From:","CSeq:","Contact:","Route:","Record-Route:","Max-Forwards:","Allow:","Require:","Supported:"});
            // Remove our Authentication header if it's there.
            foreach(SIP_SingleValueHF<SIP_t_Credentials> header in request.ProxyAuthorization.HeaderFields){
                try{
                    Auth_HttpDigest digest = new Auth_HttpDigest(header.ValueX.AuthData,request.RequestLine.Method);
                    if(m_pOwner.Stack.Realm == digest.Realm){
                        request.ProxyAuthorization.Remove(header);
                    }
                }
                catch{
                    // We don't care errors here. This can happen if remote server xxx auth method here and
                    // we don't know how to parse it, so we leave it as is.
                }
            }

            SIP_ClientTransaction clientTransaction = m_pCallee.CreateTransaction(request);
            clientTransaction.ResponseReceived += new EventHandler<SIP_ResponseReceivedEventArgs>(m_pCallee_ResponseReceived);
            clientTransaction.Tag = e.ServerTransaction;
            clientTransaction.Start();*/
        }
                
        #endregion

        #region method m_pCaller_Terminated

        /// <summary>
        /// This method is called when caller dialog has terminated, normally this happens 
        /// when dialog gets BYE request.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pCaller_Terminated(object sender,EventArgs e)
        {
            Terminate();
        }

        #endregion

        #region method m_pCallee_ResponseReceived

        /// <summary>
        /// This method is called when callee dialog client transaction receives response.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pCallee_ResponseReceived(object sender,SIP_ResponseReceivedEventArgs e)
        {
            SIP_ServerTransaction serverTransaction = (SIP_ServerTransaction)e.ClientTransaction.Tag;
            //SIP_Response response = serverTransaction.Request.CreateResponse(e.Response.StatusCode_ReasonPhrase);
            //CopyMessage(e.Response,response,new string[]{"Via:","Call-Id:","To:","From:","CSeq:","Contact:","Route:","Record-Route:","Allow:","Supported:"});
            //serverTransaction.SendResponse(response);
        }

        #endregion


        #region method m_pCallee_RequestReceived

        /// <summary>
        /// Is called when callee sends new request.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void m_pCallee_RequestReceived(SIP_RequestReceivedEventArgs e)
        {    /*
            SIP_Request request = m_pCaller.CreateRequest(e.Request.RequestLine.Method);
            CopyMessage(e.Request,request,new string[]{"Via:","Call-Id:","To:","From:","CSeq:","Contact:","Route:","Record-Route:","Max-Forwards:","Allow:","Require:","Supported:"});
            // Remove our Authentication header if it's there.
            foreach(SIP_SingleValueHF<SIP_t_Credentials> header in request.ProxyAuthorization.HeaderFields){
                try{
                    Auth_HttpDigest digest = new Auth_HttpDigest(header.ValueX.AuthData,request.RequestLine.Method);
                    if(m_pOwner.Stack.Realm == digest.Realm){
                        request.ProxyAuthorization.Remove(header);
                    }
                }
                catch{
                    // We don't care errors here. This can happen if remote server xxx auth method here and
                    // we don't know how to parse it, so we leave it as is.
                }
            }

            SIP_ClientTransaction clientTransaction = m_pCaller.CreateTransaction(request);
            clientTransaction.ResponseReceived += new EventHandler<SIP_ResponseReceivedEventArgs>(m_pCaller_ResponseReceived);
            clientTransaction.Tag = e.ServerTransaction;
            clientTransaction.Start();*/
        }

        #endregion

        #region method m_pCalee_Terminated

        /// <summary>
        /// This method is called when callee dialog has terminated, normally this happens 
        /// when dialog gets BYE request.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pCallee_Terminated(object sender,EventArgs e)
        {
            Terminate();
        }

        #endregion

        #region method m_pCaller_ResponseReceived

        /// <summary>
        /// This method is called when caller dialog client transaction receives response.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pCaller_ResponseReceived(object sender,SIP_ResponseReceivedEventArgs e)
        {
            SIP_ServerTransaction serverTransaction = (SIP_ServerTransaction)e.ClientTransaction.Tag;
            //SIP_Response response = serverTransaction.Request.CreateResponse(e.Response.StatusCode_ReasonPhrase);
            //CopyMessage(e.Response,response,new string[]{"Via:","Call-Id:","To:","From:","CSeq:","Contact:","Route:","Record-Route:","Allow:","Supported:"});
            //serverTransaction.SendResponse(response);
        }

        #endregion

        #endregion


        #region method Terminate

        /// <summary>
        /// Terminates call.
        /// </summary>
        public void Terminate()
        {
            if(m_IsTerminated){
                return;
            }
            m_IsTerminated = true;
   
            m_pOwner.RemoveCall(this);

            if(m_pCaller != null){
                //m_pCaller.Terminate();
                m_pCaller.Dispose();
                m_pCaller = null;
            }
            if(m_pCallee != null){
                //m_pCallee.Terminate();
                m_pCallee.Dispose();
                m_pCallee = null;
            }

            m_pOwner.OnCallTerminated(this);
        }

        #endregion

        #region method CallTransfer
        /*
        /// <summary>
        /// Transfers call to specified recipient.
        /// </summary>
        /// <param name="to">Address where to transfer call.</param>
        public void CallTransfer(string to)
        {
            throw new NotImplementedException();
        }*/

        #endregion
                
        #region method CopyMessage

        /// <summary>
        /// Copies header fileds from 1 message to antother.
        /// </summary>
        /// <param name="source">Source message.</param>
        /// <param name="destination">Destination message.</param>
        /// <param name="exceptHeaders">Header fields not to copy.</param>
        private void CopyMessage(SIP_Message source,SIP_Message destination,string[] exceptHeaders)
        {
            foreach(SIP_HeaderField headerField in source.Header){
                bool copy = true;
                foreach(string h in exceptHeaders){
                    if(h.ToLower() == headerField.Name.ToLower()){
                        copy = false;
                        break;
                    }
                }

                if(copy){
                    destination.Header.Add(headerField.Name,headerField.Value);
                }
            }

            destination.Data = source.Data;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets call start time.
        /// </summary>
        public DateTime StartTime
        {
            get{ return m_StartTime; }
        }

        /// <summary>
        /// Gets current call ID.
        /// </summary>
        public string CallID
        {
            get{ return m_CallID; }
        }

        /// <summary>
        /// Gets caller SIP dialog.
        /// </summary>
        public SIP_Dialog CallerDialog
        {
            get{ return m_pCaller; }
        }

        /// <summary>
        /// Gets callee SIP dialog.
        /// </summary>
        public SIP_Dialog CalleeDialog
        {
            get{ return m_pCallee; }
        }
        
        /// <summary>
        /// Gets if call has timed out and needs to be terminated.
        /// </summary>
        public bool IsTimedOut
        {
            // TODO:

            get{ return false; }
        }

        #endregion

    }
}
