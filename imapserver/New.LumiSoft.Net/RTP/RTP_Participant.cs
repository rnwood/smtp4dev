using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This is base class for <b>RTP_Participant_Local</b> and <b>RTP_Participant_Remote</b> class.
    /// </summary>
    public abstract class RTP_Participant
    {
        private string           m_CNAME    = "";
        private List<RTP_Source> m_pSources = null;
        private object           m_pTag     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="cname">Canonical name of participant.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>cname</b> is null reference.</exception>
        public RTP_Participant(string cname)
        {
            if(cname == null){
                throw new ArgumentNullException("cname");
            }
            if(cname == string.Empty){
                throw new ArgumentException("Argument 'cname' value must be specified.");
            }

            m_CNAME = cname;

            m_pSources = new List<RTP_Source>();
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        internal void Dispose()
        {
            m_pSources = null;

            this.Removed = null;
            this.SourceAdded = null;
            this.SourceRemoved = null;
        }

        #endregion


        #region method EnsureSource

        /// <summary>
        /// Adds specified source to participant if participant doesn't contain the specified source.
        /// </summary>
        /// <param name="source">RTP source.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>source</b> is null reference.</exception>
        internal void EnsureSource(RTP_Source source)
        {
            if(source == null){
                throw new ArgumentNullException("source");
            }

            if(!m_pSources.Contains(source)){
                m_pSources.Add(source);

                OnSourceAdded(source);

                source.Disposing += new EventHandler(delegate(object sender,EventArgs e){
                    if(m_pSources.Remove(source)){
                        OnSourceRemoved(source);

                        // If last source removed, the participant is dead, so dispose participant.
                        if(m_pSources.Count == 0){
                            OnRemoved();
                            Dispose();
                        }
                    }
                });
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets canonical name of participant.
        /// </summary>
        public string CNAME
        {
            get{ return m_CNAME; }
        }

        /// <summary>
        /// Gets the sources what participant owns(sends).
        /// </summary>
        public RTP_Source[] Sources
        {
            get{ return m_pSources.ToArray(); }
        }

        /// <summary>
        /// Gets or sets user data.
        /// </summary>
        public object Tag
        {
            get{ return m_pTag; }

            set{ m_pTag = value; }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when participant disjoins(timeout or BYE all sources) the RTP multimedia session.
        /// </summary>
        public event EventHandler Removed = null;

        #region method OnRemoved

        /// <summary>
        /// Raises <b>Removed</b> event.
        /// </summary>
        private void OnRemoved()
        {
            if(this.Removed != null){
                this.Removed(this,new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// Is raised when participant gets new RTP source.
        /// </summary>
        public event EventHandler<RTP_SourceEventArgs> SourceAdded = null;

        #region method OnSourceAdded

        /// <summary>
        /// Raises <b>SourceAdded</b> event.
        /// </summary>
        /// <param name="source">RTP source.</param>
        private void OnSourceAdded(RTP_Source source)
        {
            if(source == null){
                throw new ArgumentNullException("source");
            }

            if(this.SourceAdded != null){
                this.SourceAdded(this,new RTP_SourceEventArgs(source));
            }
        }

        #endregion

        /// <summary>
        /// Is raised when RTP source removed from(Timeout or BYE) participant.
        /// </summary>
        public event EventHandler<RTP_SourceEventArgs> SourceRemoved = null;

        #region method OnSourceRemoved

        /// <summary>
        /// Raises <b>SourceRemoved</b> event.
        /// </summary>
        /// <param name="source">RTP source.</param>
        private void OnSourceRemoved(RTP_Source source)
        {
            if(source == null){
                throw new ArgumentNullException("source");
            }

            if(this.SourceRemoved != null){
                this.SourceRemoved(this,new RTP_SourceEventArgs(source));
            }
        }

        #endregion

        #endregion
    }
}
