using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;

namespace LumiSoft.Net.WebDav.Client
{
    /// <summary>
    /// Implements WebDav client. Defined in RFC 4918.
    /// </summary>
    public class WebDav_Client
    {
        private NetworkCredential m_pCredentials = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public WebDav_Client()
        {
        }


        #region method PropFind

        /// <summary>
        /// Executes PROPFIND method.
        /// </summary>
        /// <param name="requestUri">Request URI.</param>
        /// <param name="propertyNames">Properties to get. Value null means property names listing.</param>
        /// <param name="depth">Maximum depth inside collections to get.</param>
        /// <returns>Returns server returned responses.</returns>
        public WebDav_MultiStatus PropFind(string requestUri,string[] propertyNames,int depth)
        {
            if(requestUri == null){
                throw new ArgumentNullException("requestUri");
            }

            StringBuilder requestContentString = new StringBuilder();
            requestContentString.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n");
            requestContentString.Append("<propfind xmlns=\"DAV:\">\r\n");
            requestContentString.Append("<prop>\r\n");
            if(propertyNames == null || propertyNames.Length == 0){
                requestContentString.Append("   <propname/>\r\n");
            }
            else{
                foreach(string propertyName in propertyNames){
                    requestContentString.Append("<" + propertyName + "/>");
                }
            }            
            requestContentString.Append("</prop>\r\n");
            requestContentString.Append("</propfind>\r\n");

            byte[] requestContent = Encoding.UTF8.GetBytes(requestContentString.ToString());

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            request.Method = "PROPFIND";
            request.ContentType = "application/xml";
            request.ContentLength = requestContent.Length;
            request.Credentials = m_pCredentials;
            if(depth > -1){
                request.Headers.Add("Depth: " + depth);
            }
            request.GetRequestStream().Write(requestContent,0,requestContent.Length);
            
            return WebDav_MultiStatus.Parse(request.GetResponse().GetResponseStream());
        }

        #endregion

        #region method PropPatch

        // public void PropPatch()
        // {
        // }

        #endregion

        #region method MkCol

        /// <summary>
        /// Creates new collection to the specified path.
        /// </summary>
        /// <param name="uri">Target collection URI.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>uri</b> null reference.</exception>
        public void MkCol(string uri)
        {
            if(uri == null){
                throw new ArgumentNullException("uri");
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.Method = "MKCOL";
            request.Credentials = m_pCredentials;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        }

        #endregion

        #region method Get
        
        /// <summary>
        /// Gets the specified resource stream.
        /// </summary>
        /// <param name="uri">Target resource URI.</param>
        /// <param name="contentSize">Returns resource size in bytes.</param>
        /// <returns>Retruns resource stream.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>uri</b> is null reference.</exception>
        public Stream Get(string uri,out long contentSize)
        {
            if(uri == null){
                throw new ArgumentNullException("uri");
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.Method = "GET";
            request.Credentials = m_pCredentials;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            contentSize = response.ContentLength;

            return response.GetResponseStream();
        }

        #endregion

        #region method Head

        // public void Head()
        // {
        // }

        #endregion

        #region method Post

        // public void Post()
        // {
        // }

        #endregion

        #region method Delete

        /// <summary>
        /// Deletes specified resource.
        /// </summary>
        /// <param name="uri">Target URI. For example: htt://server/test.txt .</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>uri</b> is null reference.</exception>
        public void Delete(string uri)
        {
            if(uri == null){
                throw new ArgumentNullException("uri");
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(uri);
            request.Method = "DELETE";
            request.Credentials = m_pCredentials;

            request.GetResponse();
        }

        #endregion

        #region method Put

        /// <summary>
        /// Creates specified resource to the specified location.
        /// </summary>
        /// <param name="targetUri">Target URI. For example: htt://server/test.txt .</param>
        /// <param name="stream">Stream which data to upload.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>targetUri</b> or <b>stream</b> is null reference.</exception>
        public void Put(string targetUri,Stream stream)
        {
            if(targetUri == null){
                throw new ArgumentNullException("targetUri");
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            // Work around, to casuse authentication, otherwise we may not use AllowWriteStreamBuffering = false later.
            // All this because ms is so lazy, tries to write all data to memory, instead switching to temp file if bigger 
            // data sent.
            try{
                HttpWebRequest dummy  = (HttpWebRequest)HttpWebRequest.Create(targetUri);
			    // Set the username and the password.
			    dummy.Credentials = m_pCredentials;
			    dummy.PreAuthenticate = true;
			    dummy.Method = "HEAD";
			    ((HttpWebResponse)dummy.GetResponse()).Close(); 
            }
            catch{
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUri);
            request.Method = "PUT";
            request.ContentType = "application/octet-stream";
            request.Credentials = m_pCredentials;
            request.PreAuthenticate = true;
            request.AllowWriteStreamBuffering = false;
            if(stream.CanSeek){                
                request.ContentLength = (stream.Length - stream.Position);
            }            
            
            using(Stream requestStream = request.GetRequestStream()){                
                Net_Utils.StreamCopy(stream,requestStream,32000);
            }

            request.GetResponse();
        }

        #endregion

        #region method Copy

        /// <summary>
        /// Copies source URI resource to the target URI.
        /// </summary>
        /// <param name="sourceUri">Source URI.</param>
        /// <param name="targetUri">Target URI.</param>
        /// <param name="depth">If source is collection, then depth specified how many nested levels will be copied.</param>
        /// <param name="overwrite">If true and target resource already exists, it will be over written. 
        /// If false and target resource exists, exception is thrown.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>sourceUri</b> or <b>targetUri</b> is null reference.</exception>
        public void Copy(string sourceUri,string targetUri,int depth,bool overwrite)
        {
            if(sourceUri == null){
                throw new ArgumentNullException(sourceUri);
            }
            if(targetUri == null){
                throw new ArgumentNullException(targetUri);
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(sourceUri);
            request.Method = "COPY";
            request.Headers.Add("Destination: " + targetUri);
            request.Headers.Add("Overwrite: " + (overwrite ? "T" : "F"));
            if(depth > -1){
                request.Headers.Add("Depth: " + depth);
            }
            request.Credentials = m_pCredentials;

            request.GetResponse();
        }

        #endregion

        #region method Move

        /// <summary>
        /// Moves source URI resource to the target URI.
        /// </summary>
        /// <param name="sourceUri">Source URI.</param>
        /// <param name="targetUri">Target URI.</param>
        /// <param name="depth">If source is collection, then depth specified how many nested levels will be copied.</param>
        /// <param name="overwrite">If true and target resource already exists, it will be over written. 
        /// If false and target resource exists, exception is thrown.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>sourceUri</b> or <b>targetUri</b> is null reference.</exception>
        public void Move(string sourceUri,string targetUri,int depth,bool overwrite)
        {
            if(sourceUri == null){
                throw new ArgumentNullException(sourceUri);
            }
            if(targetUri == null){
                throw new ArgumentNullException(targetUri);
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(sourceUri);
            request.Method = "MOVE";
            request.Headers.Add("Destination: " + targetUri);
            request.Headers.Add("Overwrite: " + (overwrite ? "T" : "F"));
            if(depth > -1){
                request.Headers.Add("Depth: " + depth);
            }
            request.Credentials = m_pCredentials;

            request.GetResponse();
        }

        #endregion

        #region method Lock

        // public void Lock()
        // {
        // }

        #endregion

        #region method Unlock

        // public void Unlock()
        // {
        // }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets or sets credentials.
        /// </summary>
        public NetworkCredential Credentials
        {
            get{ return m_pCredentials; }

            set{ m_pCredentials = value; }
        }

        #endregion

    }
}
