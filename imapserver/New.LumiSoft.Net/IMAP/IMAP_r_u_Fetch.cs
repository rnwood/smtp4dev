using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;
using LumiSoft.Net.IMAP.Client;
using LumiSoft.Net.IMAP.Server;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP FETCH response. Defined in RFC 3501 7.4.2.
    /// </summary>
    public class IMAP_r_u_Fetch : IMAP_r_u
    {
        private int                    m_MsgSeqNo   = 0;
        private List<IMAP_t_Fetch_r_i> m_pDataItems = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="msgSeqNo">Message 1-based sequence number.</param>
        /// <param name="dataItems">Fetch response data-items.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>dataItems</b> is null reference.</exception>
        public IMAP_r_u_Fetch(int msgSeqNo,IMAP_t_Fetch_r_i[] dataItems)
        {
            if(msgSeqNo < 1){
                throw new ArgumentException("Argument 'msgSeqNo' value must be >= 1.","msgSeqNo");
            }
            if(dataItems == null){
                throw new ArgumentNullException("dataItems");
            }

            m_MsgSeqNo = msgSeqNo;

            m_pDataItems = new List<IMAP_t_Fetch_r_i>();
            m_pDataItems.AddRange(dataItems);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="msgSeqNo">Message 1-based sequence number.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        internal IMAP_r_u_Fetch(int msgSeqNo)
        {
            if(msgSeqNo < 1){
                throw new ArgumentException("Argument 'msgSeqNo' value must be >= 1.","msgSeqNo");
            }

            m_MsgSeqNo = msgSeqNo;

            m_pDataItems = new List<IMAP_t_Fetch_r_i>();
        }


        #region method ParseAsync

        /// <summary>
        /// Starts parsing FETCH response.
        /// </summary>
        /// <param name="imap">IMAP cleint.</param>
        /// <param name="line">Initial FETCH response line.</param>
        /// <param name="callback">Callback to be called when fetch completed.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>imap</b>,<b>line</b> or <b>callback</b> is null reference.</exception>
        internal void ParseAsync(IMAP_Client imap,string line,EventHandler<EventArgs<Exception>> callback)
        {
            if(imap == null){
                throw new ArgumentNullException("imap");
            }
            if(line == null){
                throw new ArgumentNullException("line");
            }
            if(callback == null){
                throw new ArgumentNullException("callback");
            }

            /* RFC 3501 7.4.2. FETCH Response.
                Example:    S: * 23 FETCH (FLAGS (\Seen) RFC822.SIZE 44827)
            */
                        
            StringReader r = new StringReader(line);

            // Eat '*'
            r.ReadWord();
            // Parse seqNo
            m_MsgSeqNo = Convert.ToInt32(r.ReadWord());
            // Eat 'FETCH'
            r.ReadWord();
            // Eat '(', if list of fetch data-items.
            r.ReadToFirstChar();
            if(r.StartsWith("(")){
                r.ReadSpecifiedLength(1);
            }

            ParseDataItems(imap,r,callback);
        }

        #endregion


        #region override method ToStreamAsync

        /// <summary>
        /// Starts writing response to the specified stream.
        /// </summary>
        /// <param name="session">Owner IMAP session.</param>
        /// <param name="stream">Stream where to store response.</param>
        /// <param name="mailboxEncoding">Specifies how mailbox name is encoded.</param>
        /// <param name="completedAsyncCallback">Callback to be called when this method completes asynchronously.</param>
        /// <returns>Returns true is method completed asynchronously(the completedAsyncCallback is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        protected override bool ToStreamAsync(IMAP_Session session,Stream stream,IMAP_Mailbox_Encoding mailboxEncoding,EventHandler<EventArgs<Exception>> completedAsyncCallback)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            StringBuilder buffer = new StringBuilder();
            buffer.Append("* " + m_MsgSeqNo + " FETCH (");

            for(int i=0;i<m_pDataItems.Count;i++){
                IMAP_t_Fetch_r_i dataItem = m_pDataItems[i];

                if(i > 0){
                    buffer.Append(" ");
                }

                if(dataItem is IMAP_t_Fetch_r_i_Flags){
                    buffer.Append("FLAGS (" + ((IMAP_t_Fetch_r_i_Flags)dataItem).Flags.ToString() + ")");
                }
                else if(dataItem is IMAP_t_Fetch_r_i_Uid){
                    buffer.Append("UID " + ((IMAP_t_Fetch_r_i_Uid)dataItem).UID.ToString());
                }
                else{
                    throw new NotImplementedException("Fetch response data-item '" + dataItem.ToString() + "' not implemented.");
                }
            }

            buffer.Append(")\r\n");
            
            string responseS = buffer.ToString();
            byte[] response  = Encoding.UTF8.GetBytes(responseS);

            // Log.
            if(session != null){
                session.LogAddWrite(response.Length,responseS.TrimEnd());
            }

            // Starts writing response to stream.
            IAsyncResult ar = stream.BeginWrite(
                response,
                0,
                response.Length,
                delegate(IAsyncResult r){                    
                    if(r.CompletedSynchronously){
                        return;
                    }

                    try{
                        stream.EndWrite(r);

                        if(completedAsyncCallback != null){
                            completedAsyncCallback(this,new EventArgs<Exception>(null));
                        }
                    }
                    catch(Exception x){
                        if(completedAsyncCallback != null){
                            completedAsyncCallback(this,new EventArgs<Exception>(x));
                        }
                    }
                },
                null
            );
            // Completed synchronously, process result.
            if(ar.CompletedSynchronously){
                stream.EndWrite(ar);

                return false;
            }
            // Completed asynchronously, stream.BeginWrite AsyncCallback will continue processing.
            else{
                return true;
            }
        }

        #endregion


        #region method ParseDataItems

        /// <summary>
        /// Starts parsing fetch data-items,
        /// </summary>
        /// <param name="imap">IMAP client.</param>
        /// <param name="r">Fetch line reader.</param>
        /// <param name="callback">Callback to be called when parsing completes.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>imap</b>,<b>r</b> or <b>callback</b> is null reference.</exception>
        private void ParseDataItems(IMAP_Client imap,StringReader r,EventHandler<EventArgs<Exception>> callback)
        {
            if(imap == null){
                throw new ArgumentNullException("imap");
            }
            if(r == null){
                throw new ArgumentNullException("r");
            }
            if(callback == null){
                throw new ArgumentNullException("callback");
            }

            /* RFC 3501 7.4.2. FETCH Response.
                Example:    S: * 23 FETCH (FLAGS (\Seen) RFC822.SIZE 44827)
            */
                        
            while(true){
                r.ReadToFirstChar();

                #region BODY[]

                if(r.StartsWith("BODY[",false)){
                    /* RFC 3501 7.4.2. FETCH Response.
                        BODY[<section>]<<origin octet>>
                         A string expressing the body contents of the specified section.
                         The string SHOULD be interpreted by the client according to the
                         content transfer encoding, body type, and subtype.

                         If the origin octet is specified, this string is a substring of
                         the entire body contents, starting at that origin octet.  This
                         means that BODY[]<0> MAY be truncated, but BODY[] is NEVER
                         truncated.

                            Note: The origin octet facility MUST NOT be used by a server
                            in a FETCH response unless the client specifically requested
                            it by means of a FETCH of a BODY[<section>]<<partial>> data
                            item.

                         8-bit textual data is permitted if a [CHARSET] identifier is
                         part of the body parameter parenthesized list for this section.
                         Note that headers (part specifiers HEADER or MIME, or the
                         header portion of a MESSAGE/RFC822 part), MUST be 7-bit; 8-bit
                         characters are not permitted in headers.  Note also that the
                         [RFC-2822] delimiting blank line between the header and the
                         body is not affected by header line subsetting; the blank line
                         is always included as part of header data, except in the case
                         of a message which has no body and no blank line.

                         Non-textual data such as binary data MUST be transfer encoded
                         into a textual form, such as BASE64, prior to being sent to the
                         client.  To derive the original binary data, the client MUST
                         decode the transfer encoded string.
                    */

                    // Eat BODY word.
                    r.ReadWord();

                    // Read body-section.
                    string section = r.ReadParenthesized();

                    // Read origin if any.
                    int offset = -1;
                    if(r.StartsWith("<")){
                        offset = Convert.ToInt32(r.ReadParenthesized().Split(' ')[0]);
                    }                                                     
                    
                    IMAP_t_Fetch_r_i_Body dataItem = new IMAP_t_Fetch_r_i_Body(section,offset,new MemoryStreamEx(32000));
                    m_pDataItems.Add(dataItem);
                                        
                    // Raise event, allow user to specify store stream.
                    IMAP_Client_e_FetchGetStoreStream eArgs = new IMAP_Client_e_FetchGetStoreStream(this,dataItem);
                    imap.OnFetchGetStoreStream(eArgs);
                    // User specified own stream, use it.
                    if(eArgs.Stream != null){                        
                        dataItem.Stream.Dispose();
                        dataItem.SetStream(eArgs.Stream);
                    }

                    // Read data will complete async and will continue data-items parsing, exit this method.
                    if(ReadData(imap,r,callback,dataItem.Stream)){
                        return;
                    }
                    // Continue processing.
                    //else{
                }

                #endregion

                #region BODY

                else if(r.StartsWith("BODY ",false)){
                    //IMAP_t_Fetch_r_i_BodyS
                }

                #endregion

                #region BODYSTRUCTURE

                else if(r.StartsWith("BODYSTRUCTURE",false)){
                    //IMAP_t_Fetch_r_i_BodyStructure
                }

                #endregion

                #region ENVELOPE

                else if(r.StartsWith("ENVELOPE",false)){
                    // Envelope can contain string literals, we just try to parse it.
                    // If parse fails, just get string literal and try again as long as all ENVELOPE data has read.
                                        
                    string envelope = null;
                    while(true){ 
                        // Create temporary reader(we don't want to read partial ENVELOPE data from reader).
                        StringReader tmpReader = new StringReader(r.SourceString);

                        // Eat ENVELOPE word.
                        tmpReader.ReadWord();
                        tmpReader.ReadToFirstChar();

                        try{
                            envelope = tmpReader.ReadParenthesized();
                            // We got full ENVELOPE, so use tmp reader as reader.
                            r = tmpReader;
                                                        
                            break;
                        }
                        catch{                            
                            // Read completed async, it will continue parsing.
                            if(ReadStringLiteral(imap,r,callback)){
                                return;
                            }
                        }                       
                    }

                    m_pDataItems.Add(IMAP_t_Fetch_r_i_Envelope.Parse(new StringReader(envelope)));
                }

                #endregion

                #region FLAGS

                else if(r.StartsWith("FLAGS",false)){
                    /* RFC 3501 7.4.2. FETCH Response.
                        FLAGS
                            A parenthesized list of flags that are set for this message.
                    */

                    // Eat FLAGS word.
                    r.ReadWord();

                    m_pDataItems.Add(new IMAP_t_Fetch_r_i_Flags(IMAP_t_MsgFlags.Parse(r.ReadParenthesized())));
                }

                #endregion

                #region INTERNALDATE

                else if(r.StartsWith("INTERNALDATE",false)){
                    /* RFC 3501 7.4.2. FETCH Response.
                        INTERNALDATE
                            A string representing the internal date of the message.
                    */

                    // Eat INTERNALDATE word.
                    r.ReadWord();

                    m_pDataItems.Add(new IMAP_t_Fetch_r_i_InternalDate(IMAP_Utils.ParseDate(r.ReadWord())));
                }

                #endregion

                #region RFC822

                else if(r.StartsWith("RFC822 ",false)){
                    /* RFC 3501 7.4.2. FETCH Response.
                        RFC822
                            Equivalent to BODY[].
                    */

                    // Eat RFC822 word.
                    r.ReadWord();
                    r.ReadToFirstChar();

                    IMAP_t_Fetch_r_i_Rfc822 dataItem = new IMAP_t_Fetch_r_i_Rfc822(new MemoryStreamEx(32000));
                    m_pDataItems.Add(dataItem);
                                        
                    // Raise event, allow user to specify store stream.
                    IMAP_Client_e_FetchGetStoreStream eArgs = new IMAP_Client_e_FetchGetStoreStream(this,dataItem);
                    imap.OnFetchGetStoreStream(eArgs);
                    // User specified own stream, use it.
                    if(eArgs.Stream != null){                        
                        dataItem.Stream.Dispose();
                        dataItem.SetStream(eArgs.Stream);
                    }

                    // Read data will complete async and will continue data-items parsing, exit this method.
                    if(ReadData(imap,r,callback,dataItem.Stream)){
                        return;
                    }
                    // Continue processing.
                    //else{
                }

                #endregion

                #region RFC822.HEADER

                else if(r.StartsWith("RFC822.HEADER",false)){
                    /* RFC 3501 7.4.2. FETCH Response.
                        RFC822.HEADER
                            Equivalent to BODY[HEADER].  Note that this did not result in
                            \Seen being set, because RFC822.HEADER response data occurs as
                            a result of a FETCH of RFC822.HEADER.  BODY[HEADER] response
                            data occurs as a result of a FETCH of BODY[HEADER] (which sets
                            \Seen) or BODY.PEEK[HEADER] (which does not set \Seen).
                    */

                    // Eat RFC822.HEADER word.
                    r.ReadWord();
                    r.ReadToFirstChar();

                    IMAP_t_Fetch_r_i_Rfc822Header dataItem = new IMAP_t_Fetch_r_i_Rfc822Header(new MemoryStreamEx(32000));
                    m_pDataItems.Add(dataItem);
                                        
                    // Raise event, allow user to specify store stream.
                    IMAP_Client_e_FetchGetStoreStream eArgs = new IMAP_Client_e_FetchGetStoreStream(this,dataItem);
                    imap.OnFetchGetStoreStream(eArgs);
                    // User specified own stream, use it.
                    if(eArgs.Stream != null){                        
                        dataItem.Stream.Dispose();
                        dataItem.SetStream(eArgs.Stream);
                    }

                    // Read data will complete async and will continue data-items parsing, exit this method.
                    if(ReadData(imap,r,callback,dataItem.Stream)){
                        return;
                    }
                    // Continue processing.
                    //else{
                }

                #endregion

                #region RFC822.SIZE

                else if(r.StartsWith("RFC822.SIZE",false)){
                    /* RFC 3501 7.4.2. FETCH Response.
                        RFC822.SIZE
                            A number expressing the [RFC-2822] size of the message.
                    */

                    // Eat RFC822.SIZE word.
                    r.ReadWord();

                    m_pDataItems.Add(new IMAP_t_Fetch_r_i_Rfc822Size(Convert.ToInt32(r.ReadWord())));
                }

                #endregion

                #region RFC822.TEXT

                else if(r.StartsWith("RFC822.TEXT",false)){
                    /* RFC 3501 7.4.2. FETCH Response.
                        RFC822.TEXT
                            Equivalent to BODY[TEXT].
                    */

                    // Eat RFC822.TEXT word.
                    r.ReadWord();
                    r.ReadToFirstChar();

                    IMAP_t_Fetch_r_i_Rfc822Text dataItem = new IMAP_t_Fetch_r_i_Rfc822Text(new MemoryStreamEx(32000));
                    m_pDataItems.Add(dataItem);
                                        
                    // Raise event, allow user to specify store stream.
                    IMAP_Client_e_FetchGetStoreStream eArgs = new IMAP_Client_e_FetchGetStoreStream(this,dataItem);
                    imap.OnFetchGetStoreStream(eArgs);
                    // User specified own stream, use it.
                    if(eArgs.Stream != null){                        
                        dataItem.Stream.Dispose();
                        dataItem.SetStream(eArgs.Stream);
                    }

                    // Read data will complete async and will continue data-items parsing, exit this method.
                    if(ReadData(imap,r,callback,dataItem.Stream)){
                        return;
                    }
                    // Continue processing.
                    //else{
                }

                #endregion

                #region UID

                else if(r.StartsWith("UID",false)){
                    /* RFC 3501 7.4.2. FETCH Response.
                        UID
                            A number expressing the unique identifier of the message.
                    */

                    // Eat UID word.
                    r.ReadWord();

                    m_pDataItems.Add(new IMAP_t_Fetch_r_i_Uid(Convert.ToInt64(r.ReadWord())));
                }

                #endregion

                #region X-GM-MSGID

                else if(r.StartsWith("X-GM-MSGID",false)){
                    /* http://code.google.com/intl/et/apis/gmail/imap X-GM-MSGID.
                
                    */

                    // Eat X-GM-MSGID word.
                    r.ReadWord();

                    m_pDataItems.Add(new IMAP_t_Fetch_r_i_X_GM_MSGID(Convert.ToUInt64(r.ReadWord())));
                }

                #endregion

                #region X-GM-THRID

                else if(r.StartsWith("X-GM-THRID",false)){
                    /* http://code.google.com/intl/et/apis/gmail/imap X-GM-THRID.
                
                    */

                    // Eat X-GM-THRID word.
                    r.ReadWord();

                    m_pDataItems.Add(new IMAP_t_Fetch_r_i_X_GM_THRID(Convert.ToUInt64(r.ReadWord())));
                }

                #endregion

                #region ) - fetch closing.

                else if(r.StartsWith(")",false)){
                    break;
                }

                #endregion

                else{
                    throw new ParseException("Not supported FETCH data-item '" + r.ReadToEnd() + "'.");
                }
            }

            callback(this,new EventArgs<Exception>(null));
        }

        #endregion

        #region method ReadStringLiteral

        /// <summary>
        /// Reads string-literal(stores it to reader 'r') and continuing fetch line.
        /// </summary>
        /// <param name="imap">IMAP client.</param>
        /// <param name="r">String reader.</param>
        /// <param name="callback">Fetch completion callback.</param>
        /// <returns>Returns true if completed asynchronously or false if completed synchronously.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>imap</b>,<b>r</b> or <b>callback</b> is null reference.</exception>
        private bool ReadStringLiteral(IMAP_Client imap,StringReader r,EventHandler<EventArgs<Exception>> callback)
        {
            if(imap == null){
                throw new ArgumentNullException("imap");
            }
            if(r == null){
                throw new ArgumentNullException("r");
            }
            if(callback == null){
                throw new ArgumentNullException("callback");
            }

            if(r.SourceString.EndsWith("}") && r.SourceString.IndexOf("{") > -1){
                MemoryStream stream = new MemoryStream();
                string size = r.SourceString.Substring(r.SourceString.LastIndexOf("{") + 1,r.SourceString.Length - r.SourceString.LastIndexOf("{") - 2);
                // Remove {n} from string.
                r.RemoveFromEnd(r.SourceString.Length - r.SourceString.LastIndexOf('{'));
                                
                IMAP_Client.ReadStringLiteralAsyncOP op = new IMAP_Client.ReadStringLiteralAsyncOP(stream,Convert.ToInt32(size));
                op.CompletedAsync += delegate(object sender,EventArgs<IMAP_Client.ReadStringLiteralAsyncOP> e){
                    try{
                        // Read string literal failed.
                        if(op.Error != null){
                            callback(this,new EventArgs<Exception>(op.Error));
                        }
                        else{
                            // Append string-literal to fetch reader.
                            r.AppendString(TextUtils.QuoteString(Encoding.UTF8.GetString(stream.ToArray())));

                            // Read next fetch line completed synchronously.
                            if(!ReadNextFetchLine(imap,r,callback)){
                                ParseDataItems(imap,r,callback);
                            }
                        }
                    }
                    catch(Exception x){
                        callback(this,new EventArgs<Exception>(x));
                    }
                    finally{
                        op.Dispose();
                    }
                };

                // Read string literal completed sync.
                if(!imap.ReadStringLiteralAsync(op)){
                    try{
                        // Read string literal failed.
                        if(op.Error != null){
                            callback(this,new EventArgs<Exception>(op.Error));

                            return true;
                        }
                        else{
                            // Append string-literal to fetch reader.
                            r.AppendString(TextUtils.QuoteString(Encoding.UTF8.GetString(stream.ToArray())));

                            return ReadNextFetchLine(imap,r,callback);
                        }
                    }
                    finally{
                        op.Dispose();
                    }
                }
                // Read string literal completed async.
                else{
                    return true;
                }
            }
            else{
                throw new ParseException("No string-literal available '" + r.SourceString + "'.");
            }
        }

        #endregion

        #region method ReadData

        /// <summary>
        /// Reads IMAP string(string-literal,quoted-string,NIL) and remaining FETCH line if needed.
        /// </summary>
        /// <param name="imap">IMAP client.</param>
        /// <param name="r">Fetch line reader.</param>
        /// <param name="callback">Fetch completion callback.</param>
        /// <param name="stream">Stream where to store readed data.</param>
        /// <returns>Returns true if completed asynchronously or false if completed synchronously.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>imap</b>,<b>r</b>,<b>callback</b> or <b>stream</b> is null reference.</exception>
        private bool ReadData(IMAP_Client imap,StringReader r,EventHandler<EventArgs<Exception>> callback,Stream stream)
        {
            if(imap == null){
                throw new ArgumentNullException("imap");
            }
            if(r == null){
                throw new ArgumentNullException("r");
            }
            if(callback == null){
                throw new ArgumentNullException("callback");
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            r.ReadToFirstChar();

            // We don't have data.
            if(r.StartsWith("NIL",false)){
                // Eat NIL.
                r.ReadWord();

                return false;
            }
            // Data value is returned as string-literal.
            else if(r.StartsWith("{",false)){
                IMAP_Client.ReadStringLiteralAsyncOP op = new IMAP_Client.ReadStringLiteralAsyncOP(stream,Convert.ToInt32(r.ReadParenthesized()));
                op.CompletedAsync += delegate(object sender,EventArgs<IMAP_Client.ReadStringLiteralAsyncOP> e){
                    try{
                        // Read string literal failed.
                        if(op.Error != null){
                            callback(this,new EventArgs<Exception>(op.Error));
                        }
                        else{
                            // Read next fetch line completed synchronously.
                            if(!ReadNextFetchLine(imap,r,callback)){
                                ParseDataItems(imap,r,callback);
                            }
                        }
                    }
                    catch(Exception x){
                        callback(this,new EventArgs<Exception>(x));
                    }
                    finally{
                        op.Dispose();
                    }
                };

                // Read string literal completed sync.
                if(!imap.ReadStringLiteralAsync(op)){
                    try{
                        // Read string literal failed.
                        if(op.Error != null){
                            callback(this,new EventArgs<Exception>(op.Error));

                            return true;
                        }
                        else{
                            // Read next fetch line completed synchronously.
                            if(!ReadNextFetchLine(imap,r,callback)){
                                return false;
                            }
                            else{
                                return true;
                            }
                        }
                    }
                    finally{
                        op.Dispose();
                    }
                }
                // Read string literal completed async.
                else{
                    return true;
                }
            }
            // Data is quoted-string.
            else{
                byte[] data = Encoding.UTF8.GetBytes(r.ReadWord());
                stream.Write(data,0,data.Length);

                return false;
            }
        }

        #endregion

        #region method ReadNextFetchLine
        
        /// <summary>
        /// Reads next continuing FETCH line and stores to fetch reader 'r'.
        /// </summary>
        /// <param name="imap">IMAP client.</param>
        /// <param name="r">String reader.</param>
        /// <param name="callback">Fetch completion callback.</param>
        /// <returns>Returns true if completed asynchronously or false if completed synchronously.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>imap</b>,<b>r</b> or <b>callback</b> is null reference.</exception>
        private bool ReadNextFetchLine(IMAP_Client imap,StringReader r,EventHandler<EventArgs<Exception>> callback)
        {
            if(imap == null){
                throw new ArgumentNullException("imap");
            }
            if(r == null){
                throw new ArgumentNullException("r");
            }
            if(callback == null){
                throw new ArgumentNullException("callback");
            }

            SmartStream.ReadLineAsyncOP readLineOP = new SmartStream.ReadLineAsyncOP(new byte[64000],SizeExceededAction.JunkAndThrowException);
            readLineOP.Completed += delegate(object sender,EventArgs<SmartStream.ReadLineAsyncOP> e){
                try{
                    // Read line failed.
                    if(readLineOP.Error != null){
                        callback(this,new EventArgs<Exception>(readLineOP.Error));
                    }
                    else{
                        // Log.
                        imap.LogAddRead(readLineOP.BytesInBuffer,readLineOP.LineUtf8);
                        
                        // Append fetch line to fetch reader.
                        r.AppendString(readLineOP.LineUtf8);

                        ParseDataItems(imap,r,callback);
                    }
                }
                catch(Exception x){
                    callback(this,new EventArgs<Exception>(x));
                }
                finally{
                    readLineOP.Dispose();
                }
            };

            // Read line completed synchronously.
            if(imap.TcpStream.ReadLine(readLineOP,true)){
                try{
                    // Read line failed.
                    if(readLineOP.Error != null){
                        callback(this,new EventArgs<Exception>(readLineOP.Error));

                        return true;
                    }
                    else{
                        // Log.
                        imap.LogAddRead(readLineOP.BytesInBuffer,readLineOP.LineUtf8);

                        // Append fetch line to fetch reader.
                        r.AppendString(readLineOP.LineUtf8);

                        return false;
                    }
                }
                finally{
                    readLineOP.Dispose();
                }
            }

            return true;
        }

        #endregion


        #region method FilterDataItem

        /// <summary>
        /// Returns specified data-item or null if no such item.
        /// </summary>
        /// <param name="dataItem">Data-item to filter.</param>
        /// <returns>Returns specified data-item or null if no such item.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>dataItem</b> is null reference.</exception>
        private IMAP_t_Fetch_r_i FilterDataItem(Type dataItem)
        {
            if(dataItem == null){
                throw new ArgumentNullException("dataItem");
            }

            foreach(IMAP_t_Fetch_r_i item in m_pDataItems){
                if(item.GetType() == dataItem){
                    return item;
                }
            }

            return null;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets message 1-based sequence number.
        /// </summary>
        public int SeqNo
        {
            get{ return m_MsgSeqNo; }
        }

        /// <summary>
        /// Gets fetch response data items.
        /// </summary>
        public IMAP_t_Fetch_r_i[] DataItems
        {
            get{ return m_pDataItems.ToArray(); }
        }

        /// <summary>
        /// Gets BODY[] values.
        /// </summary>
        public IMAP_t_Fetch_r_i_Body[] Body
        {
            get{
                List<IMAP_t_Fetch_r_i_Body> retVal = new List<IMAP_t_Fetch_r_i_Body>();
                foreach(IMAP_t_Fetch_r_i item in m_pDataItems){
                    if(item is IMAP_t_Fetch_r_i_Body){
                        retVal.Add((IMAP_t_Fetch_r_i_Body)item);
                    }
                }
                return retVal.ToArray(); 
            }
        }
        
        // BODYSTRUCTURE

        /// <summary>
        /// Gets ENVELOPE value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_Envelope Envelope
        {
            get{ return (IMAP_t_Fetch_r_i_Envelope)FilterDataItem(typeof(IMAP_t_Fetch_r_i_Envelope)); }
        }

        /// <summary>
        /// Gets FLAGS value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_Flags Flags
        {
            get{ return (IMAP_t_Fetch_r_i_Flags)FilterDataItem(typeof(IMAP_t_Fetch_r_i_Flags)); }
        }

        /// <summary>
        /// Gets INTERNALDATE value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_InternalDate InternalDate
        {
            get{ return (IMAP_t_Fetch_r_i_InternalDate)FilterDataItem(typeof(IMAP_t_Fetch_r_i_InternalDate)); }
        }

        /// <summary>
        /// Gets RFC822 value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_Rfc822 Rfc822
        {
            get{ return (IMAP_t_Fetch_r_i_Rfc822)FilterDataItem(typeof(IMAP_t_Fetch_r_i_Rfc822)); }
        }

        /// <summary>
        /// Gets RFC822.HEADER value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_Rfc822Header Rfc822Header
        {
            get{ return (IMAP_t_Fetch_r_i_Rfc822Header)FilterDataItem(typeof(IMAP_t_Fetch_r_i_Rfc822Header)); }
        }

        /// <summary>
        /// Gets RFC822.SIZE value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_Rfc822Size Rfc822Size
        {
            get{ return (IMAP_t_Fetch_r_i_Rfc822Size)FilterDataItem(typeof(IMAP_t_Fetch_r_i_Rfc822Size)); }
        }

        /// <summary>
        /// Gets RFC822.TEXT value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_Rfc822Text Rfc822Text
        {
            get{ return (IMAP_t_Fetch_r_i_Rfc822Text)FilterDataItem(typeof(IMAP_t_Fetch_r_i_Rfc822Text)); }
        }

        /// <summary>
        /// Gets UID value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_Uid UID
        {
            get{ return (IMAP_t_Fetch_r_i_Uid)FilterDataItem(typeof(IMAP_t_Fetch_r_i_Uid)); }
        }

        /// <summary>
        /// Gets X-GM-MSGID value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_X_GM_MSGID X_GM_MSGID
        {
            get{ return (IMAP_t_Fetch_r_i_X_GM_MSGID)FilterDataItem(typeof(IMAP_t_Fetch_r_i_X_GM_MSGID)); }
        }

        /// <summary>
        /// Gets X-GM-THRID value. Returns null if fetch response doesn't contain specified data-item.
        /// </summary>
        public IMAP_t_Fetch_r_i_X_GM_THRID X_GM_THRID
        {
            get{ return (IMAP_t_Fetch_r_i_X_GM_THRID)FilterDataItem(typeof(IMAP_t_Fetch_r_i_X_GM_THRID)); }
        }

        #endregion
    }
}
