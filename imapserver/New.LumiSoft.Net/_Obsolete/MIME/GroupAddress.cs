using System;

namespace LumiSoft.Net.Mime
{
	/// <summary>
	/// RFC 2822 3.4. (Address Specification) Group address.
	/// <p/>
	/// Syntax: display-name':'[mailbox *(',' mailbox)]';'
	/// </summary>
    [Obsolete("See LumiSoft.Net.MIME or LumiSoft.Net.Mail namepaces for replacement.")]
	public class GroupAddress : Address
	{
		private string                   m_DisplayName   = "";
		private MailboxAddressCollection m_pGroupMembers = null;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public GroupAddress() : base(true)
		{
			m_pGroupMembers = new MailboxAddressCollection();
			m_pGroupMembers.Owner = this;
		}


		#region static method Parse
		
		/// <summary>
		/// Parses Rfc 2822 3.4 group address from group address string. Syntax: display-name':'[mailbox *(',' mailbox)]';'
		/// </summary>
		/// <param name="group">Group address string.</param>
		/// <returns></returns>
		public static GroupAddress Parse(string group)
		{
			GroupAddress g = new GroupAddress();

			// Syntax: display-name':'[mailbox *(',' mailbox)]';'
			string[] parts = TextUtils.SplitQuotedString(group,':');
			if(parts.Length > -1){
				g.DisplayName = TextUtils.UnQuoteString(parts[0]);				
			}
			if(parts.Length > 1){
				g.GroupMembers.Parse(parts[1]);
			}

			return g;
		}

		#endregion


		#region internal method OnChanged

		/// <summary>
		/// This called when group address has changed.
		/// </summary>
		internal void OnChanged()
		{
			if(this.Owner != null){
				if(this.Owner is AddressList){
					((AddressList)this.Owner).OnCollectionChanged();
				}				
			}
		}

		#endregion


		#region Properties implementation

		/// <summary>
		/// Gets Group as RFC 2822(3.4. Address Specification) string. Syntax: display-name':'[mailbox *(',' mailbox)]';'
		/// </summary>
		public string GroupString
		{
			get{ return TextUtils.QuoteString(this.DisplayName) + ":" + this.GroupMembers.ToMailboxListString() + ";"; }
		}

		/// <summary>
		/// Gets or sets display name.
		/// </summary>
		public string DisplayName
		{
			get{ return m_DisplayName; }

			set{ 
				m_DisplayName = value; 

				OnChanged();
			}
		}

		/// <summary>
		/// Gets group members collection.
		/// </summary>
		public MailboxAddressCollection GroupMembers
		{
			get{ return m_pGroupMembers; }
		}

		#endregion

	}
}
