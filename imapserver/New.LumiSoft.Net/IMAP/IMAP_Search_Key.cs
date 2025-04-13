using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using LumiSoft.Net.IMAP.Client;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class is base class for IMAP SEARCH search-key. Defined in RFC 3501 6.4.4.
    /// </summary>
    public abstract class IMAP_Search_Key
    {
        #region static method ParseKey

        /// <summary>
        /// Parses one search key or search key group.
        /// </summary>
        /// <param name="r">String reader.</param>
        /// <returns>Returns one parsed search key or search key group.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>r</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when parsing fails.</exception>
        internal static IMAP_Search_Key ParseKey(StringReader r)
        {
            if(r == null){
                throw new ArgumentNullException("r");
            }
            
            r.ReadToFirstChar();

            // Keys group
            if(r.StartsWith("(",false)){
                return IMAP_Search_Key_Group.Parse(new StringReader(r.ReadParenthesized()));
            }
            // ANSWERED
            else if(r.StartsWith("ANSWERED",false)){
                return IMAP_Search_Key_Answered.Parse(r);
            }
            // BCC
            else if(r.StartsWith("BCC",false)){
                return IMAP_Search_Key_Bcc.Parse(r);
            }
            // BEFORE
            else if(r.StartsWith("BEFORE",false)){
                return IMAP_Search_Key_Before.Parse(r);
            }
            // BODY
            else if(r.StartsWith("BODY",false)){
                return IMAP_Search_Key_Body.Parse(r);
            }
            // CC
            else if(r.StartsWith("CC",false)){
                return IMAP_Search_Key_Cc.Parse(r);
            }
            // DELETED
            else if(r.StartsWith("DELETED",false)){
                return IMAP_Search_Key_Deleted.Parse(r);
            }
            // DRAFT
            else if(r.StartsWith("DRAFT",false)){
                return IMAP_Search_Key_Draft.Parse(r);
            }
            // FLAGGED
            else if(r.StartsWith("FLAGGED",false)){
                return IMAP_Search_Key_Flagged.Parse(r);
            }
            // FROM
            else if(r.StartsWith("FROM",false)){
                return IMAP_Search_Key_From.Parse(r);
            }
            // HEADER
            else if(r.StartsWith("HEADER",false)){
                return IMAP_Search_Key_Header.Parse(r);
            }
            // KEYWORD
            else if(r.StartsWith("KEYWORD",false)){
                return IMAP_Search_Key_Keyword.Parse(r);
            }
            // LARGER
            else if(r.StartsWith("LARGER",false)){
                return IMAP_Search_Key_Larger.Parse(r);
            }
            // NEW
            else if(r.StartsWith("NEW",false)){
                return IMAP_Search_Key_New.Parse(r);
            }
            // NOT
            else if(r.StartsWith("NOT",false)){
                return IMAP_Search_Key_Not.Parse(r);
            }
            // OLD
            else if(r.StartsWith("OLD",false)){
                return IMAP_Search_Key_Old.Parse(r);
            }
            // ON
            else if(r.StartsWith("ON",false)){
                return IMAP_Search_Key_On.Parse(r);
            }
            // OR
            else if(r.StartsWith("OR",false)){
                return IMAP_Search_Key_Or.Parse(r);
            }
            // RECENT
            else if(r.StartsWith("RECENT",false)){
                return IMAP_Search_Key_Recent.Parse(r);
            }
            // SEEN
            else if(r.StartsWith("SEEN",false)){
                return IMAP_Search_Key_Seen.Parse(r);
            }
            // SENTBEFORE
            else if(r.StartsWith("SENTBEFORE",false)){
                return IMAP_Search_Key_SentBefore.Parse(r);
            }
            // SENTON
            else if(r.StartsWith("SENTON",false)){
                return IMAP_Search_Key_SentOn.Parse(r);
            }
            // SENTSINCE
            else if(r.StartsWith("SENTSINCE",false)){
                return IMAP_Search_Key_SentSince.Parse(r);
            }
            // SEQSET
            else if(r.StartsWith("SEQSET",false)){
                return IMAP_Search_Key_SeqSet.Parse(r);
            }
            // SINCE
            else if(r.StartsWith("SINCE",false)){
                return IMAP_Search_Key_Since.Parse(r);
            }
            // SMALLER
            else if(r.StartsWith("SMALLER",false)){
                return IMAP_Search_Key_Smaller.Parse(r);
            }
            // SUBJECT
            else if(r.StartsWith("SUBJECT",false)){
                return IMAP_Search_Key_Subject.Parse(r);
            }
            // TEXT
            else if(r.StartsWith("TEXT",false)){
                return IMAP_Search_Key_Text.Parse(r);
            }
            // TO
            else if(r.StartsWith("TO",false)){
                return IMAP_Search_Key_To.Parse(r);
            }
            // UID
            else if(r.StartsWith("UID",false)){
                return IMAP_Search_Key_Uid.Parse(r);
            }
            // UNANSWERED
            else if(r.StartsWith("UNANSWERED",false)){
                return IMAP_Search_Key_Unanswered.Parse(r);
            }
            // UNDELETED
            else if(r.StartsWith("UNDELETED",false)){
                return IMAP_Search_Key_Undeleted.Parse(r);
            }
            // UNDRAFT
            else if(r.StartsWith("UNDRAFT",false)){
                return IMAP_Search_Key_Undraft.Parse(r);
            }
            // UNFLAGGED
            else if(r.StartsWith("UNFLAGGED",false)){
                return IMAP_Search_Key_Unflagged.Parse(r);
            }
            // UNKEYWORD
            else if(r.StartsWith("UNKEYWORD",false)){
                return IMAP_Search_Key_Unkeyword.Parse(r);
            }
            // UNSEEN
            else if(r.StartsWith("UNSEEN",false)){
                return IMAP_Search_Key_Unseen.Parse(r);
			}            
            // ALL
			else if (r.StartsWith("ALL", false))
			{
				return IMAP_Search_Key_All.Parse(r);
			}
			else
			{
                // Check if we hae sequence-set. Because of IMAP specification sucks a little here, why the hell they didn't 
                // do the keyword(SEQSET) for it, like UID. Now we just have to try if it is sequence-set or BAD key. 
                try{
                   return IMAP_Search_Key_SeqSet.Parse(r);
                }
                catch{
                   throw new ParseException("Unknown search key '" + r.ReadToEnd() + "'.");
                }
            }
        }

        #endregion


        #region internal abstract method ToCmdParts

        /// <summary>
        /// Stores IMAP search-key command parts to the specified array.
        /// </summary>
        /// <param name="list">Array where to store command parts.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>list</b> is null reference.</exception>
        internal abstract void ToCmdParts(List<IMAP_Client_CmdPart> list);

        #endregion
    }
}
