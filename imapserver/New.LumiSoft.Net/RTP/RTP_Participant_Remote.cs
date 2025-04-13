using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// Implements RTP <b>participant</b>. Defined in RFC 3550.
    /// </summary>
    public class RTP_Participant_Remote : RTP_Participant
    {
        private string m_Name     = null;
        private string m_Email    = null;
        private string m_Phone    = null;
        private string m_Location = null;
        private string m_Tool     = null;
        private string m_Note     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="cname">Canonical name of participant. For example: john.doe@domain.com-randomTag.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>cname</b> is null reference.</exception>
        internal RTP_Participant_Remote(string cname) : base(cname)
        {
        }


        #region method ToString

        /// <summary>
        /// Returns participant as string.
        /// </summary>
        /// <returns>Returns participant as string.</returns>
        public override string ToString()
        {
            StringBuilder retVal = new StringBuilder();

            retVal.AppendLine("CNAME: " + this.CNAME);            
            if(!string.IsNullOrEmpty(m_Name)){
                retVal.AppendLine("Name: " + m_Name);
            }
            if(!string.IsNullOrEmpty(m_Email)){
                retVal.AppendLine("Email: " + m_Email);
            }
            if(!string.IsNullOrEmpty(m_Phone)){
                retVal.AppendLine("Phone: " + m_Phone);
            }
            if(!string.IsNullOrEmpty(m_Location)){
                retVal.AppendLine("Location: " + m_Location);
            }
            if(!string.IsNullOrEmpty(m_Tool)){
                retVal.AppendLine("Tool: " + m_Tool);
            }
            if(!string.IsNullOrEmpty(m_Note)){
                retVal.AppendLine("Note: " + m_Note);
            }

            return retVal.ToString().TrimEnd();
        }

        #endregion


        #region method Update

        /// <summary>
        /// Updates participant data from SDES items.
        /// </summary>
        /// <param name="sdes">SDES chunk.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>sdes</b> is null reference value.</exception>
        internal void Update(RTCP_Packet_SDES_Chunk sdes)
        {
            if(sdes == null){
                throw new ArgumentNullException("sdes");
            }

            bool changed = false;
            if(!string.IsNullOrEmpty(sdes.Name) && !string.Equals(m_Name,sdes.Name)){
                m_Name = sdes.Name;
                changed = true;
            }
            if(!string.IsNullOrEmpty(sdes.Email) && !string.Equals(m_Email,sdes.Email)){
                m_Email = sdes.Email;
                changed = true;
            }
            if(!string.IsNullOrEmpty(sdes.Phone) && !string.Equals(Phone,sdes.Phone)){
                m_Phone = sdes.Phone;
                changed = true;
            }
            if(!string.IsNullOrEmpty(sdes.Location) && !string.Equals(m_Location,sdes.Location)){
                m_Location = sdes.Location;
                changed = true;
            }
            if(!string.IsNullOrEmpty(sdes.Tool) && !string.Equals(m_Tool,sdes.Tool)){
                m_Tool = sdes.Tool;
                changed = true;
            }
            if(!string.IsNullOrEmpty(sdes.Note) && !string.Equals(m_Note,sdes.Note)){
                m_Note = sdes.Note;
                changed = true;
            }

            if(changed){
                OnChanged();
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets the real name, eg. "John Doe". Value null means not specified.
        /// </summary>
        public string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets email address. For example "John.Doe@example.com". Value null means not specified.
        /// </summary>
        public string Email
        {
            get{ return m_Email; }
        }

        /// <summary>
        /// Gets phone number. For example "+1 908 555 1212". Value null means not specified.
        /// </summary>
        public string Phone
        {
            get{ return m_Phone; }
        }

        /// <summary>
        /// Gets location string. It may be geographic address or for example chat room name.
        /// Value null means not specified.
        /// </summary>
        public string Location
        {
            get{ return m_Location; }
        }

        /// <summary>
        /// Gets streaming application name/version.
        /// Value null means not specified.
        /// </summary>
        public string Tool
        {
            get{ return m_Tool; }
        }

        /// <summary>
        /// Gets note text. The NOTE item is intended for transient messages describing the current state
        /// of the source, e.g., "on the phone, can't talk". Value null means not specified.
        /// </summary>
        public string Note
        {
            get{ return m_Note; }
        }

        // TODO: PRIV
                
        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when participant data changed.
        /// </summary>
        public event EventHandler<RTP_ParticipantEventArgs> Changed = null;

        #region method OnChanged

        /// <summary>
        /// Raises <b>Changed</b> event.
        /// </summary>
        private void OnChanged()
        {
            if(this.Changed != null){
                this.Changed(this,new RTP_ParticipantEventArgs(this));
            }
        }

        #endregion

        #endregion

    }
}
