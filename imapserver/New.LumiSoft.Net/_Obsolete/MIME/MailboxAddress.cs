using System;

using LumiSoft.Net.MIME;

namespace LumiSoft.Net.Mime
{
    /// <summary>
    /// RFC 2822 3.4. (Address Specification) Mailbox address. 
    /// <p/>
    /// Syntax: ["display-name"&lt;SP&gt;]&lt;local-part@domain&gt;.
    /// </summary>
    [Obsolete("See LumiSoft.Net.MIME or LumiSoft.Net.Mail namepaces for replacement.")]
    public class MailboxAddress : Address
    {
        private string m_DisplayName = "";
        private string m_EmailAddress = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MailboxAddress() : base(false)
        {
        }

        /// <summary>
        /// Creates new mailbox from specified email address.
        /// </summary>
        /// <param name="emailAddress">Email address.</param>
        public MailboxAddress(string emailAddress) : base(false)
        {
            m_EmailAddress = emailAddress;
        }

        /// <summary>
        /// Creates new mailbox from specified name and email address.
        /// </summary>
        /// <param name="displayName">Display name.</param>
        /// <param name="emailAddress">Email address.</param>
        public MailboxAddress(string displayName, string emailAddress) : base(false)
        {
            if (!string.IsNullOrEmpty(displayName))
                m_DisplayName = displayName;
            m_EmailAddress = emailAddress;
        }


        #region method Parse

        /// <summary>
        /// Parses mailbox from mailbox address string.
        /// </summary>
        /// <param name="mailbox">Mailbox string. Format: ["diplay-name"&lt;SP&gt;]&lt;local-part@domain&gt;.</param>
        /// <returns></returns>
        public static MailboxAddress Parse(string mailbox)
        {
            mailbox = mailbox.Trim();

            /* We must parse following situations:
				"Ivar Lumi" <ivar@lumisoft.ee>
				"Ivar Lumi" ivar@lumisoft.ee
				<ivar@lumisoft.ee>
				ivar@lumisoft.ee				
				Ivar Lumi <ivar@lumisoft.ee>
			*/

            string name = "";
            string emailAddress = mailbox;

            // Email address is between <> and remaining left part is display name
            if (mailbox.IndexOf("<") > -1 && mailbox.IndexOf(">") > -1)
            {
                name = MIME_Encoding_EncodedWord.DecodeS(TextUtils.UnQuoteString(mailbox.Substring(0, mailbox.LastIndexOf("<"))));
                emailAddress = mailbox.Substring(mailbox.LastIndexOf("<") + 1, mailbox.Length - mailbox.LastIndexOf("<") - 2).Trim();
            }
            else
            {
                // There is name included, parse it
                if (mailbox.StartsWith("\""))
                {
                    int startIndex = mailbox.IndexOf("\"");
                    if (startIndex > -1 && mailbox.LastIndexOf("\"") > startIndex)
                    {
                        name = MIME_Encoding_EncodedWord.DecodeS(mailbox.Substring(startIndex + 1, mailbox.LastIndexOf("\"") - startIndex - 1).Trim());
                    }

                    emailAddress = mailbox.Substring(mailbox.LastIndexOf("\"") + 1).Trim();
                }

                // Right part must be email address
                emailAddress = emailAddress.Replace("<", "").Replace(">", "").Trim();
            }

            return new MailboxAddress(name, emailAddress);
        }

        #endregion


        #region method ToMailboxAddressString

        /// <summary>
        /// Converts this to valid mailbox address string.
        /// Defined in RFC 2822(3.4. Address Specification) string. Format: ["display-name"&lt;SP&gt;]&lt;local-part@domain&gt;.
        /// For example, "Ivar Lumi" &lt;ivar@lumisoft.ee&gt;.
        /// If display name contains unicode chrs, display name will be encoded with canonical encoding in utf-8 charset.
        /// </summary>
        /// <returns></returns>
        public string ToMailboxAddressString()
        {
            string retVal = "";
            if (m_DisplayName.Length > 0)
            {
                if (Core.IsAscii(m_DisplayName))
                {
                    retVal = TextUtils.QuoteString(m_DisplayName) + " ";
                }
                else
                {
                    // Encoded word must be treated as unquoted and unescaped word.
                    retVal = MimeUtils.EncodeWord(m_DisplayName) + " ";
                }
            }
            retVal += "<" + this.EmailAddress + ">";

            return retVal;
        }

        #endregion


        #region internal method OnChanged

        /// <summary>
		/// This called when mailox address has changed.
		/// </summary>
		internal void OnChanged()
        {
            if (this.Owner != null)
            {
                if (this.Owner is AddressList)
                {
                    ((AddressList)this.Owner).OnCollectionChanged();
                }
                else if (this.Owner is MailboxAddressCollection)
                {
                    ((MailboxAddressCollection)this.Owner).OnCollectionChanged();
                }
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets Mailbox as RFC 2822(3.4. Address Specification) string. Format: ["display-name"&lt;SP&gt;]&lt;local-part@domain&gt;.
        /// For example, "Ivar Lumi" &lt;ivar@lumisoft.ee&gt;.
        /// </summary>
        [Obsolete("Use ToMailboxAddressString instead !")]
        public string MailboxString
        {
            get
            {
                string retVal = "";
                if (this.DisplayName != "")
                {
                    retVal += TextUtils.QuoteString(this.DisplayName) + " ";
                }
                retVal += "<" + this.EmailAddress + ">";

                return retVal;
            }
        }

        /// <summary>
        /// Gets or sets display name. 
        /// </summary>
        public string DisplayName
        {
            get { return m_DisplayName; }

            set
            {
                m_DisplayName = value;

                OnChanged();
            }
        }

        /// <summary>
        /// Gets or sets email address. For example ivar@lumisoft.ee.
        /// </summary>
        public string EmailAddress
        {
            get { return m_EmailAddress; }

            set
            {
                // Email address can contain only ASCII chars.
                if (!Core.IsAscii(value))
                {
                    throw new Exception("Email address can contain ASCII chars only !");
                }

                m_EmailAddress = value;

                OnChanged();
            }
        }

        /// <summary>
        /// Gets local-part from email address. For example mailbox is "ivar" from "ivar@lumisoft.ee".
        /// </summary>
        public string LocalPart
        {
            get
            {
                if (this.EmailAddress.IndexOf("@") > -1)
                {
                    return this.EmailAddress.Substring(0, this.EmailAddress.IndexOf("@"));
                }
                else
                {
                    return this.EmailAddress;
                }
            }
        }

        /// <summary>
        /// Gets domain from email address. For example domain is "lumisoft.ee" from "ivar@lumisoft.ee".
        /// </summary>
        public string Domain
        {
            get
            {
                if (this.EmailAddress.IndexOf("@") != -1)
                {
                    return this.EmailAddress.Substring(this.EmailAddress.IndexOf("@") + 1);
                }
                else
                {
                    return "";
                }
            }
        }

        #endregion

    }
}
