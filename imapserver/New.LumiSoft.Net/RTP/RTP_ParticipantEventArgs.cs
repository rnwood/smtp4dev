using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This claass provides data for <b>RTP_MultimediaSession.NewParticipant</b> event.
    /// </summary>
    public class RTP_ParticipantEventArgs : EventArgs
    {
        private RTP_Participant_Remote m_pParticipant = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="participant">RTP participant.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>participant</b> is null reference.</exception>
        public RTP_ParticipantEventArgs(RTP_Participant_Remote participant)
        {
            if(participant == null){
                throw new ArgumentNullException("participant");
            }

            m_pParticipant = participant;
        }


        #region Properties implementation

        /// <summary>
        /// Gets participant.
        /// </summary>
        public RTP_Participant_Remote Participant
        {
            get{ return m_pParticipant; }
        }

        #endregion

    }
}
