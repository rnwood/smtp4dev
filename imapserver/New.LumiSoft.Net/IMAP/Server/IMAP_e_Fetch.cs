using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.Mail;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.Fetch">IMAP_Session.Fetch</b> event.
    /// </summary>
    /// <remarks>
    /// IMAP FETCH handler application should provide requested data for each message in <see cref="IMAP_e_Fetch.MessagesInfo"/>
    /// by calling <see cref="IMAP_e_Fetch.AddData(IMAP_MessageInfo,Mail_Message)"/> method.
    /// </remarks>
    public class IMAP_e_Fetch : EventArgs
    {
        #region class e_NewMessageData

        /// <summary>
        /// This class provides data for <b cref="IMAP_e_Fetch.NewMessageData">IMAP_Session.NewMessageData</b> event.
        /// </summary>
        internal class e_NewMessageData : EventArgs
        {            
            private IMAP_MessageInfo m_pMsgInfo = null;
            private Mail_Message     m_pMsgData = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="msgInfo">Message info.</param>
            /// <param name="msgData">Message data stream.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>msgInfo</b> is null reference.</exception>
            public e_NewMessageData(IMAP_MessageInfo msgInfo,Mail_Message msgData)
            {
                if(msgInfo == null){
                    throw new ArgumentNullException("msgInfo");
                }

                m_pMsgInfo = msgInfo;
                m_pMsgData = msgData;
            }


            #region Properties implementation

            /// <summary>
            /// Gets message info.
            /// </summary>
            public IMAP_MessageInfo MessageInfo
            {
                get{ return m_pMsgInfo; }
            }

            /// <summary>
            /// Gets message data stream.
            /// </summary>
            public Mail_Message MessageData
            {
                get{ return m_pMsgData; }
            }

            #endregion
        }

        #endregion

        private IMAP_r_ServerStatus m_pResponse     = null;
        private IMAP_MessageInfo[]  m_pMessagesInfo = null;
        private IMAP_Fetch_DataType m_FetchDataType = IMAP_Fetch_DataType.FullMessage;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="messagesInfo">Messages info.</param>
        /// <param name="fetchDataType">Fetch data type(Specifies what data AddData method expects).</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>messagesInfo</b> or <b>response</b> is null reference.</exception>
        internal IMAP_e_Fetch(IMAP_MessageInfo[] messagesInfo,IMAP_Fetch_DataType fetchDataType,IMAP_r_ServerStatus response)
        {
            if(messagesInfo == null){
                throw new ArgumentNullException("messagesInfo");
            }
            if(response == null){
                throw new ArgumentNullException("response");
            }

            m_pMessagesInfo = messagesInfo;
            m_FetchDataType = fetchDataType;
            m_pResponse     = response;
        }


        #region method AddData

        /// <summary>
        /// Adds specified message for FETCH response processing.
        /// </summary>
        /// <param name="msgInfo">IMAP message info.</param>
        internal void AddData(IMAP_MessageInfo msgInfo)
        {
            OnNewMessageData(msgInfo,null);
        }

        /// <summary>
        /// Adds specified message for FETCH response processing.
        /// </summary>
        /// <param name="msgInfo">IMAP message info which message data it is.</param>
        /// <param name="msgData">Message data. NOTE: This value must be as specified by <see cref="IMAP_e_Fetch.FetchDataType"/>.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>msgInfo</b> or <b>msgData</b> is null reference.</exception>
        public void AddData(IMAP_MessageInfo msgInfo,Mail_Message msgData)
        {
            if(msgInfo == null){
                throw new ArgumentNullException("msgInfo");
            }
            if(msgData == null){
                throw new ArgumentNullException("msgData");
            }

            OnNewMessageData(msgInfo,msgData);
        }

        #endregion


        #region Properties impelementation

        /// <summary>
        /// Gets or sets IMAP server response to this operation.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null reference value set.</exception>
        public IMAP_r_ServerStatus Response
        {
            get{ return m_pResponse; }

            set{ 
                if(value == null){
                    throw new ArgumentNullException("value");
                }

                m_pResponse = value; 
            }
        }

        /// <summary>
        /// Gets messages info.
        /// </summary>
        public IMAP_MessageInfo[] MessagesInfo
        {
            get{ return m_pMessagesInfo; }
        }

        /// <summary>
        /// Gets fetch data type(Specifies what data AddData method expects).
        /// </summary>
        public IMAP_Fetch_DataType FetchDataType
        {
            get{ return m_FetchDataType; }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// This event is raised when new message-info/message-data is added for FETCH processing.
        /// </summary>
        internal event EventHandler<IMAP_e_Fetch.e_NewMessageData> NewMessageData = null;

        #region method OnNewMessageData

        /// <summary>
        /// Raises <b>NewMessageData</b> event.
        /// </summary>
        /// <param name="msgInfo">IMAP message info which message data it is.</param>
        /// <param name="msgData">Message data. NOTE: This value must be as specified by <see cref="IMAP_e_Fetch.FetchDataType"/>.</param>
        private void OnNewMessageData(IMAP_MessageInfo msgInfo,Mail_Message msgData)
        {
            if(this.NewMessageData != null){
                this.NewMessageData(this,new e_NewMessageData(msgInfo,msgData));
            }
        }

        #endregion

        #endregion
    }
}
