using LinqKit;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Rnwood.Smtp4dev.DbModel;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Subjects;

namespace Rnwood.Smtp4dev.Server.Imap
{
    public class ImapSearchTranslator
    {

        public Expression<Func<DbModel.Message, bool>> Translate(IMAP_Search_Key criteria)
        {
            return criteria switch
            {
                IMAP_Search_Key_Group group => HandleGroup(group),
                IMAP_Search_Key_Unseen unseen => HandleUnseen(unseen),
                IMAP_Search_Key_Seen seen => HandleSeen(seen),
                IMAP_Search_Key_Not not => HandleNot(not),
                IMAP_Search_Key_From from => HandleFrom(from),
                IMAP_Search_Key_To to => HandleTo(to),
                IMAP_Search_Key_Subject subject => HandleSubject(subject),
                IMAP_Search_Key_Header header => HandleHeader(header),
                IMAP_Search_Key_Or or => HandleOr(or),
                IMAP_Search_Key_All all => HandleAll(all),
                IMAP_Search_Key_Draft => HandleNone(),
                IMAP_Search_Key_Flagged => HandleNone(),
                IMAP_Search_Key_Deleted => HandleNone(),
                IMAP_Search_Key_Since since => HandleSince(since),
                IMAP_Search_Key_Younger younger => HandleYounger(younger),
                IMAP_Search_Key_Older older => HandleOlder(older),
                IMAP_Search_Key_Uid uid => HandleUid(uid),
                { } unknown => throw new ImapSearchCriteriaNotSupportedException($"The criteria '{unknown} is not supported'")
            };
        }

        private Expression<Func<Message, bool>> HandleSince(IMAP_Search_Key_Since since)
        {
            return m => m.ReceivedDate >= since.Date;
        }

        private Expression<Func<Message, bool>> HandleYounger(IMAP_Search_Key_Younger younger)
        {
            // Capture the current time as a parameter that EF can properly translate
            var now = DateTime.Now;
            return m => m.ReceivedDate >= now.AddSeconds(-younger.Interval);
        }

        private Expression<Func<Message, bool>> HandleOlder(IMAP_Search_Key_Older older)
        {
            // Capture the current time as a parameter that EF can properly translate
            var now = DateTime.Now;
            return m => m.ReceivedDate < now.AddSeconds(-older.Interval);
        }

        private Expression<Func<Message, bool>> HandleUid(IMAP_Search_Key_Uid uid)
        {
            // Convert the sequence set to a list of individual UIDs that EF can translate
            // We need to enumerate the ranges and create an in-memory list
            var uidList = new List<long>();
            foreach (var range in uid.Value.Items)
            {
                for (long i = range.Start; i <= range.End; i++)
                {
                    uidList.Add(i);
                }
            }
            
            // Now use the list which EF Core can translate to SQL IN clause
            return m => uidList.Contains(m.ImapUid);
        }

        private Expression<Func<Message, bool>> HandleNone()
        {
            return m => false;
        }

        private Expression<Func<Message, bool>> HandleAll(IMAP_Search_Key_All all)
        {
            return m => true;
        }

        private Expression<Func<Message, bool>> HandleOr(IMAP_Search_Key_Or or) { 
        
            return ExpressionOptimizer.tryVisitTyped(Translate(or.SearchKey1).Or(Translate(or.SearchKey2)).Expand());
        }

        private string EscapeLike(string text)
        {
            return text.Replace("%", "[%]").Replace("?", "[?]");
        }

        private Expression<Func<Message, bool>> Contains(Expression<Func<Message, string>> property, string text)
        {
            string like = $"%{EscapeLike(text)}%";
            Expression<Func<Message, bool>> expression = m => EF.Functions.Like(property.Invoke(m), like);
            return ExpressionOptimizer.tryVisitTyped(expression.Expand());
        }

        private Expression<Func<Message, bool>> HandleSubject(IMAP_Search_Key_Subject subject)
        {
            return Contains(m => m.Subject, subject.Value);
        }

        private Expression<Func<Message, bool>> HandleTo(IMAP_Search_Key_To to)
        {
            return Contains(m => m.To, to.Value);
        }

        private Expression<Func<Message, bool>> HandleFrom(IMAP_Search_Key_From from)
        {
            return Contains(m => m.From, from.Value);
        }

        private Expression<Func<Message, bool>> HandleHeader(IMAP_Search_Key_Header header)
        {
            // For MESSAGE-ID header specifically, we can search for the value in the BodyText or Data
            // For simplicity, we'll search in the plain text representation
            // In a real implementation, we'd need to parse the MIME headers properly
            
            // For now, just search for the value anywhere in the message body text
            // This is a simplified implementation
            if (!string.IsNullOrEmpty(header.Value))
            {
                return Contains(m => m.BodyText, header.Value);
            }
            
            // If no value specified, we can't easily check if header exists without parsing
            // So we'll just return true (all messages have headers)
            return m => true;
        }

        private Expression<Func<Message, bool>> HandleNot(IMAP_Search_Key_Not not)
        {
            Expression<Func<Message, bool>> query = (m => !Translate(not.SearchKey).Invoke(m));
            return ExpressionOptimizer.tryVisitTyped(query.Expand());
        }

        private Expression<Func<Message, bool>> HandleSeen(IMAP_Search_Key_Seen seen)
        {
            return m => !m.IsUnread;
        }

        private Expression<Func<Message, bool>> HandleUnseen(IMAP_Search_Key_Unseen unseen)
        {
            return m => m.IsUnread;
        }

        private Expression<Func<Message, bool>> HandleGroup(IMAP_Search_Key_Group group)
        {
            Expression<Func<Message, bool>> result = m => true;

            foreach(var key in group.Keys)
            {
                result = result.And(Translate(key));
            }
            return ExpressionOptimizer.tryVisitTyped(result.Expand());
        }
    }


    [Serializable]
    public class ImapSearchCriteriaNotSupportedException : Exception
    {
        public ImapSearchCriteriaNotSupportedException() { }
        public ImapSearchCriteriaNotSupportedException(string message) : base(message) { }
        public ImapSearchCriteriaNotSupportedException(string message, Exception inner) : base(message, inner) { }
    
    }
}
