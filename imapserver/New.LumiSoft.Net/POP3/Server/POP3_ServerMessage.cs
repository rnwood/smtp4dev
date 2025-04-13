using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.POP3.Server
{
    /// <summary>
    /// This class represents POP3 server message.
    /// </summary>
    public class POP3_ServerMessage
    {
        private int    m_SequenceNumber      = -1;
        private string m_UID                 = "";
        private int    m_Size                = 0;
        private bool   m_IsMarkedForDeletion = false;
        private object m_pTag                = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="uid">Message UID value.</param>
        /// <param name="size">Message size in bytes.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>uid</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public POP3_ServerMessage(string uid,int size) : this(uid,size,null)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="uid">Message UID value.</param>
        /// <param name="size">Message size in bytes.</param>
        /// <param name="tag">User data.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>uid</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public POP3_ServerMessage(string uid,int size,object tag)
        {
            if(uid == null){
                throw new ArgumentNullException("uid");
            }
            if(uid == string.Empty){
                throw new ArgumentException("Argument 'uid' value must be specified.");
            }
            if(size < 0){
                throw new ArgumentException("Argument 'size' value must be >= 0.");
            }

            m_UID  = uid;
            m_Size = size;
            m_pTag = tag;
        }


        #region method SetIsMarkedForDeletion

        /// <summary>
        /// Sets IsMarkedForDeletion proerty value.
        /// </summary>
        /// <param name="value">Value.</param>
        internal void SetIsMarkedForDeletion(bool value)
        {
            m_IsMarkedForDeletion = value;
        }

        #endregion


        #region Properties implemnetation
                
        /// <summary>
        /// Gets message UID. NOTE: Before accessing this property, check that server supports UIDL command.
        /// </summary>
        public string UID
        {
            get{ return m_UID; }
        }

        /// <summary>
        /// Gets message size in bytes.
        /// </summary>
        public int Size
        {
            get{ return m_Size; }
        }

        /// <summary>
        /// Gets if message is marked for deletion.
        /// </summary>
        public bool IsMarkedForDeletion
        {
            get{ return m_IsMarkedForDeletion; }
        }

        /// <summary>
        /// Gets or sets user data.
        /// </summary>
        public object Tag
        {
            get{ return m_pTag; }

            set{ m_pTag = value; }
        }


        /// <summary>
        /// Gets message 1 based sequence number.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        internal int SequenceNumber
        {
            get{ return m_SequenceNumber; }

            set{ m_SequenceNumber = value; }
        }

        #endregion
    }
}
