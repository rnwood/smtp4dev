using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SDP
{
    /// <summary>
    /// This class represents SDP media description. Defined in RFC 4566 5.14.
    /// </summary>
    public class SDP_MediaDescription
    {
        private string                    m_MediaType     = "";
        private int                       m_Port          = 0;
        private int                       m_NumberOfPorts = 1;
        private string                    m_Protocol      = "";
        private List<string>              m_pMediaFormats = null;
        private string                    m_Information   = null;
        private SDP_Connection            m_pConnection   = null;
        private string                    m_Bandwidth     = null;
        private List<SDP_Attribute>       m_pAttributes   = null;
        private Dictionary<string,object> m_pTags         = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="mediaType">SDP media type. List of knwon values <see cref="SDP_MediaTypes"/>.</param>
        /// <param name="port">Media transport port.</param>
        /// <param name="ports">Number of continuos transport ports. </param>
        /// <param name="protocol">Gets or sets transport protocol.</param>
        /// <param name="mediaFormats">Media formats. See MediaFormats property for more info.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>mediaType</b> or <b>protocol</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SDP_MediaDescription(string mediaType,int port,int ports,string protocol,string[] mediaFormats)
        {
            if(mediaType == null){
                throw new ArgumentNullException("mediaType");
            }
            if(mediaType == string.Empty){
                throw new ArgumentException("Argument 'mediaType' value must be specified.");
            }
            if(port < 0){
                throw new ArgumentException("Argument 'port' value must be >= 0.");
            }
            if(ports < 0){
                throw new ArgumentException("Argument 'ports' value must be >= 0.");
            }
            if(protocol == null){
                throw new ArgumentNullException("protocol");
            }
            if(protocol == string.Empty){
                throw new ArgumentException("Argument 'protocol' value msut be specified.");
            }

            m_MediaType     = mediaType;
            m_Port          = port;
            m_NumberOfPorts = ports;
            m_Protocol      = protocol;

            m_pMediaFormats = new List<string>();
            m_pAttributes = new List<SDP_Attribute>();
            m_pTags = new Dictionary<string,object>();

            if(mediaFormats != null){
                m_pMediaFormats.AddRange(mediaFormats);
            }
        }

        /// <summary>
        /// Internal parse constructor.
        /// </summary>
        private SDP_MediaDescription()
        {
            m_pMediaFormats = new List<string>();
            m_pAttributes = new List<SDP_Attribute>();
            m_pTags = new Dictionary<string,object>();
        }


        #region static method Parse

        /// <summary>
        /// Parses media from "m" SDP message field.
        /// </summary>
        /// <param name="mValue">"m" SDP message field.</param>
        /// <returns></returns>
        public static SDP_MediaDescription Parse(string mValue)
        {
            SDP_MediaDescription media = new SDP_MediaDescription();

            // m=<media> <port>/<number of ports> <proto> <fmt> ...
            StringReader r = new StringReader(mValue);
            r.QuotedReadToDelimiter('=');

            //--- <media> ------------------------------------------------------------
            string word = r.ReadWord();
            if(word == null){
                throw new Exception("SDP message \"m\" field <media> value is missing !");
            }
            media.m_MediaType = word;

            //--- <port>/<number of ports> -------------------------------------------
            word = r.ReadWord();
            if(word == null){
                throw new Exception("SDP message \"m\" field <port> value is missing !");
            }
            if(word.IndexOf('/') > -1){
                string[] port_nPorts = word.Split('/');
                media.m_Port = Convert.ToInt32(port_nPorts[0]);
                media.m_NumberOfPorts = Convert.ToInt32(port_nPorts[1]);
            }
            else{
                media.m_Port = Convert.ToInt32(word);
                media.m_NumberOfPorts = 1;
            }

            //--- <proto> --------------------------------------------------------------
            word = r.ReadWord();
            if(word == null){
                throw new Exception("SDP message \"m\" field <proto> value is missing !");
            }
            media.m_Protocol = word;
            
            //--- <fmt> ----------------------------------------------------------------
            word = r.ReadWord();
            while(word != null){
                media.MediaFormats.Add(word);

                word = r.ReadWord();
            }

            return media;
        }

        #endregion


        #region method SetStreamMode

        /// <summary>
        /// Sets SDP media stream mode.
        /// </summary>
        /// <param name="streamMode">Stream mode.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>streamMode</b> is null reference.</exception>
        public void SetStreamMode(string streamMode)
        {
            if(streamMode == null){
                throw new ArgumentNullException("streamMode");
            }

            // Remove all old stream mode attributes.
            for(int i=0;i<m_pAttributes.Count;i++){
                SDP_Attribute sdpAttribute = m_pAttributes[i];
                if(string.Equals(sdpAttribute.Name,"sendrecv",StringComparison.InvariantCultureIgnoreCase)){
                    m_pAttributes.RemoveAt(i);
                    i--;
                }
                else if(string.Equals(sdpAttribute.Name,"sendonly",StringComparison.InvariantCultureIgnoreCase)){
                    m_pAttributes.RemoveAt(i);
                    i--;
                }
                else if(string.Equals(sdpAttribute.Name,"recvonly",StringComparison.InvariantCultureIgnoreCase)){
                    m_pAttributes.RemoveAt(i);
                    i--;
                }
                else if(string.Equals(sdpAttribute.Name,"inactive",StringComparison.InvariantCultureIgnoreCase)){
                    m_pAttributes.RemoveAt(i);
                    i--;
                }
            }

            if(streamMode != ""){
                m_pAttributes.Add(new SDP_Attribute(streamMode,""));
            }
        }

        #endregion

        #region method ToValue

        /// <summary>
        /// Converts this to valid media string.
        /// </summary>
        /// <returns></returns>
        public string ToValue()
        {
            /*
                m=  (media name and transport address)
                i=* (media title)
                c=* (connection information -- optional if included at session level)
                b=* (zero or more bandwidth information lines)
                k=* (encryption key)
                a=* (zero or more media attribute lines)
            */

            // m=<media> <port>/<number of ports> <proto> <fmt> ...

            StringBuilder retVal = new StringBuilder();
            if(NumberOfPorts > 1){
                retVal.Append("m=" + MediaType + " " + Port + "/" + NumberOfPorts + " " + Protocol);
            }
            else{
                retVal.Append("m=" + MediaType + " " + Port + " " + Protocol);
            }
            foreach(string mediaFormat in this.MediaFormats){
                retVal.Append(" " + mediaFormat);
            }
            retVal.Append("\r\n");
            // i (media title)
            if(!string.IsNullOrEmpty(m_Information)){
                retVal.Append("i=" + m_Information + "\r\n");
            }
            // b (bandwidth information)
            if(!string.IsNullOrEmpty(m_Bandwidth)){
                retVal.Append("b=" + m_Bandwidth + "\r\n");
            }
            // c (connection information)
            if(m_pConnection != null){
                retVal.Append(this.m_pConnection.ToValue());
            }
            foreach(SDP_Attribute attribute in this.Attributes){
                retVal.Append(attribute.ToValue());
            }

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets meadia type. Currently defined media are "audio", "video", "text", 
        /// "application", and "message", although this list may be extended in the future.
        /// </summary>
        public string MediaType
        {
            get{ return m_MediaType; }
        }

        /// <summary>
        /// Gets or sets the transport port to which the media stream is sent.
        /// </summary>
        public int Port
        {
            get{ return m_Port; }

            set{ m_Port = value; }
        }

        /// <summary>
        /// Gets or sets number of continuos media ports.
        /// </summary>
        public int NumberOfPorts
        {
            get{ return m_NumberOfPorts; }

            set{
                if(value < 1){
                    throw new ArgumentException("Property NumberOfPorts must be >= 1 !");
                }
 
                m_NumberOfPorts = value; 
            }
        }

        /// <summary>
        /// Gets or sets transport protocol. Currently known protocols: UDP;RTP/AVP;RTP/SAVP.
        /// </summary>
        public string Protocol
        {
            get{ return m_Protocol; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property Protocol cant be null or empty !");
                }

                m_Protocol = value; 
            }
        }

        /// <summary>
        /// Gets media formats collection.
        /// </summary>
        /// <remarks>
        /// <code>
        /// ; Media Formats: 
        /// ; If the Transport Protocol is "RTP/AVP" or "RTP/SAVP" the &lt;fmt&gt; 
        /// ; sub-fields contain RTP payload type numbers, for example: 
        /// ; - for Audio: 0: PCMU, 4: G723, 8: PCMA, 18: G729 
        /// ; - for Video: 31: H261, 32: MPV 
        /// ; If the Transport Protocol is "udp" the &lt;fmt&gt; sub-fields 
        /// ; must reference a MIME type 
        /// </code>
        /// </remarks>
        public List<string> MediaFormats
        {
            get{ return m_pMediaFormats; }
        }

        /// <summary>
        /// Gets or sets media information. Value null means not specified.
        /// </summary>
        public string Information
        {
            get{ return m_Information; }

            set{ m_Information = value; }
        }

        /// <summary>
        /// Gets or sets per media connection data. Value null means not specified.
        /// </summary>
        public SDP_Connection Connection
        {
            get{ return m_pConnection; }

            set{ m_pConnection = value; }
        }

        /// <summary>
        /// Gets or sets bandwidth data. Value null means not specified.
        /// </summary>
        public string Bandwidth
        {
            get{ return m_Bandwidth; }

            set{ m_Bandwidth = value; }
        }

        /// <summary>
        /// Gets media attributes collection.
        /// </summary>
        public List<SDP_Attribute> Attributes
        {
            get{ return m_pAttributes; }
        }

        /// <summary>
        /// Gets user data items collection.
        /// </summary>
        public Dictionary<string,object> Tags
        {
            get{ return m_pTags; }
        }

        #endregion

    }
}
