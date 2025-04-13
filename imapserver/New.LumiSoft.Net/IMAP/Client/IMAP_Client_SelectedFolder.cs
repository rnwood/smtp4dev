using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Client
{
    /// <summary>
    /// This class represents IMAP client selected folder.
    /// </summary>
    public class IMAP_Client_SelectedFolder
    {
        private string   m_Name                = "";
        private long     m_UidValidity         = -1;
        private string[] m_pFlags              = new string[0];
        private string[] m_pPermanentFlags     = new string[0];
        private bool     m_IsReadOnly          = false;
        private long     m_UidNext             = -1;
        private int      m_FirstUnseen         = -1;
        private int      m_MessagesCount       = 0;
        private int      m_RecentMessagesCount = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">Folder name with path.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>name</b> is null reference value.</exception>
        /// <exception cref="ArgumentException">Is riased when any of the arguments has invalid value.</exception>
        public IMAP_Client_SelectedFolder(string name)
        {
            if(name == null){
                throw new ArgumentNullException("name");
            }
            if(name == string.Empty){
                throw new ArgumentException("The argument 'name' value must be specified.","name");
            }

            m_Name = name;
        }


        #region override method ToString

        /// <summary>
        /// Returns this object as human readable string.
        /// </summary>
        /// <returns>Returns this object as human readable string.</returns>
        public override string ToString()
        {
            StringBuilder retVal = new StringBuilder();
            retVal.AppendLine("Name: "                + this.Name);
            retVal.AppendLine("UidValidity: "         + this.UidValidity);
            retVal.AppendLine("Flags: "               + StringArrayToString(this.Flags));
            retVal.AppendLine("PermanentFlags: "      + StringArrayToString(this.PermanentFlags));
            retVal.AppendLine("IsReadOnly: "          + this.IsReadOnly);
            retVal.AppendLine("UidNext: "             + this.UidNext);
            retVal.AppendLine("FirstUnseen: "         + this.FirstUnseen);
            retVal.AppendLine("MessagesCount: "       + this.MessagesCount);
            retVal.AppendLine("RecentMessagesCount: " + this.RecentMessagesCount);

            return retVal.ToString();
        }

        #endregion


        #region method SetUidValidity

        /// <summary>
        /// Sets UidValidity property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        internal void SetUidValidity(long value)
        {
            m_UidValidity = value;
        }

        #endregion

        #region method SetFlags

        /// <summary>
        /// Sets Flags property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        internal void SetFlags(string[] value)
        {
            m_pFlags = value;
        }

        #endregion

        #region method SetPermanentFlags

        /// <summary>
        /// Sets PermanentFlags property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        internal void SetPermanentFlags(string[] value)
        {
            m_pPermanentFlags = value;
        }

        #endregion

        #region method SetReadOnly

        /// <summary>
        /// Sets IsReadOnly property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        internal void SetReadOnly(bool value)
        {
            m_IsReadOnly = value;
        }

        #endregion

        #region method SetUidNext

        /// <summary>
        /// Sets UidNext property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        internal void SetUidNext(long value)
        {
            m_UidNext = value;
        }

        #endregion

        #region method SetFirstUnseen

        /// <summary>
        /// Sets FirstUnseen property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        internal void SetFirstUnseen(int value)
        {
            m_FirstUnseen = value;
        }

        #endregion

        #region method SetMessagesCount

        /// <summary>
        /// Sets MessagesCount property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        internal void SetMessagesCount(int value)
        {
            m_MessagesCount = value;
        }

        #endregion

        #region method SetRecentMessagesCount

        /// <summary>
        /// Sets RecentMessagesCount property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        internal void SetRecentMessagesCount(int value)
        {
            m_RecentMessagesCount = value;
        }

        #endregion


        #region method StringArrayToString

        /// <summary>
        /// Coneverts string array to comma separated value.
        /// </summary>
        /// <param name="value">String array.</param>
        /// <returns>Returns string array as comma separated value.</returns>
        private string StringArrayToString(string[] value)
        {
            StringBuilder retVal = new StringBuilder();

            for(int i=0;i<value.Length;i++){
                // Last item.
                if(i == (value.Length - 1)){
                    retVal.Append(value[i]);
                }
                else{
                    retVal.Append(value[i] + ",");
                }
            }

            return retVal.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets selected folder name(path included).
        /// </summary>
        public string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets folder UID value. Value null means IMAP server doesn't support <b>UIDVALIDITY</b> feature.
        /// </summary>
        public long UidValidity
        {
            get{ return m_UidValidity; }
        }

        /// <summary>
        /// Gets flags what folder supports.
        /// </summary>
        public string[] Flags
        {
            get{ return m_pFlags; }
        }

        /// <summary>
        /// Gets permanent flags what folder can store.
        /// </summary>
        public string[] PermanentFlags
        {
            get{ return m_pPermanentFlags; }
        }

        /// <summary>
        /// Gets if folder is read-only or read-write.
        /// </summary>
        public bool IsReadOnly
        {
            get{ return m_IsReadOnly; }
        }

        /// <summary>
        /// Gets next predicted message UID. Value -1 means that IMAP server doesn't support it.
        /// </summary>
        public long UidNext
        {
            get{ return m_UidNext; }
        }

        /// <summary>
        /// Gets first unseen message sequence number. Value -1 means no unseen message.
        /// </summary>
        public int FirstUnseen
        {
            get{ return m_FirstUnseen; }
        }

        /// <summary>
        /// Gets number of messages in this folder.
        /// </summary>
        public int MessagesCount
        {
            get{ return m_MessagesCount; }
        }

        /// <summary>
        /// Gets number of recent messages in this folder.
        /// </summary>
        public int RecentMessagesCount
        {
            get{ return m_RecentMessagesCount; }
        }

        #endregion
    }
}
