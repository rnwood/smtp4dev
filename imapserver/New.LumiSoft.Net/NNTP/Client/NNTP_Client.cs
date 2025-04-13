using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

using LumiSoft.Net.TCP;

namespace LumiSoft.Net.NNTP.Client
{
    /// <summary>
    /// NNTP client. Defined in RFC 977.
    /// </summary>
    public class NNTP_Client : TCP_Client
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public NNTP_Client()
        {
        }


        #region override method Disconnect

		/// <summary>
		/// Closes connection to NNTP server.
		/// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when NNTP client is not connected.</exception>
		public override void Disconnect()
		{
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("NNTP client is not connected.");
            }

			try{
                // Send QUIT command to server.                
                WriteLine("QUIT");
			}
			catch{
			}

            try{
                base.Disconnect(); 
            }
            catch{
            }
		}

		#endregion


        #region method GetNewsGroups

        /// <summary>
        /// Gets NNTP newsgoups.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when NNTP client is not connected.</exception>
        public string[] GetNewsGroups()
        {
            /* RFC 977 3.6.1.  LIST

                Returns a list of valid newsgroups and associated information.  Each
                newsgroup is sent as a line of text in the following format:

                    group last first p

                where <group> is the name of the newsgroup, <last> is the number of
                the last known article currently in that newsgroup, <first> is the
                number of the first article currently in the newsgroup, and <p> is
                either 'y' or 'n' indicating whether posting to this newsgroup is
                allowed ('y') or prohibited ('n').

                The <first> and <last> fields will always be numeric.  They may have
                leading zeros.  If the <last> field evaluates to less than the
                <first> field, there are no articles currently on file in the
                newsgroup.
              
                Example:
                   C: LIST
                   S: 215 list of newsgroups follows
                   S: net.wombats 00543 00501 y
                   S: net.unix-wizards 10125 10011 y
                   S: .
            */

            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("NNTP client is not connected.");
            }

            // Send LIST command
            WriteLine("LIST");

            // Read server response
			string responseLine = ReadLine();
            if(!responseLine.StartsWith("215")){
                throw new Exception(responseLine);
            }

            List<string> newsGroups = new List<string>();
            responseLine = ReadLine();
            while(responseLine != "."){
                newsGroups.Add(responseLine.Split(' ')[0]);

                responseLine = ReadLine();
            }

            return newsGroups.ToArray();
        }

        #endregion

        #region method PostMessage

        /// <summary>
        /// Posts specified message to the specified newsgroup.
        /// </summary>
        /// <param name="newsgroup">Newsgroup where to post message.</param>
        /// <param name="message">Message to post. Message is taken from stream current position.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when NNTP client is not connected.</exception>
        public void PostMessage(string newsgroup,Stream message)
        {
            /* RFC 977 3.10.1.  POST

                If posting is allowed, response code 340 is returned to indicate that
                the article to be posted should be sent. Response code 440 indicates
                that posting is prohibited for some installation-dependent reason.

                If posting is permitted, the article should be presented in the
                format specified by RFC850, and should include all required header
                lines. After the article's header and body have been completely sent
                by the client to the server, a further response code will be returned
                to indicate success or failure of the posting attempt.

                The text forming the header and body of the message to be posted
                should be sent by the client using the conventions for text received
                from the news server:  A single period (".") on a line indicates the
                end of the text, with lines starting with a period in the original
                text having that period doubled during transmission.

                No attempt shall be made by the server to filter characters, fold or
                limit lines, or otherwise process incoming text.  It is our intent
                that the server just pass the incoming message to be posted to the
                server installation's news posting software, which is separate from
                this specification.  See RFC850 for more details.
              
                Example:
                    C: POST
                    S: 340 Continue posting; Period on a line by itself to end
                    C: (transmits news article in RFC850 format)
                    C: .
                    S: 240 Article posted successfully.
           */

            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("NNTP client is not connected.");
            }

            // Send POST command
            WriteLine("POST");

            // Read server response
			string responseLine = ReadLine();
            if(!responseLine.StartsWith("340")){
                throw new Exception(responseLine);
            }

            // POST message
            this.TcpStream.WritePeriodTerminated(message);

            // Read server response
            responseLine = ReadLine();
            if(!responseLine.StartsWith("240")){
                throw new Exception(responseLine);
            }

        }

        #endregion
        

        #region override method OnConnected

        /// <summary>
        /// This method is called after TCP client has sucessfully connected.
        /// </summary>
        protected override void OnConnected()
        {
            // Read first line of reply, check if it's ok.
			string responseLine = ReadLine();
            if(!responseLine.StartsWith("200")){
                throw new Exception(responseLine);
            }
        }

        #endregion


        #region Properties Implementation

        
        #endregion

    }
}
