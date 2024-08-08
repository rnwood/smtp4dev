using LinqKit;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.IMAP.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Rnwood.Smtp4dev.DbModel;
using System;
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
                IMAP_Search_Key_Or or => HandleOr(or),
                IMAP_Search_Key_All all => HandleAll(all),
                IMAP_Search_Key_Draft => HandleNone(),
                IMAP_Search_Key_Flagged => HandleNone(),
                IMAP_Search_Key_Deleted => HandleNone(),
                IMAP_Search_Key_Since since => HandleSince(since),
                { } unknown => throw new ImapSearchCriteriaNotSupportedException($"The criteria '{unknown} is not supported'")
            };
        }

        private Expression<Func<Message, bool>> HandleSince(IMAP_Search_Key_Since since)
        {
            return m => m.ReceivedDate >= since.Date;
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
        protected ImapSearchCriteriaNotSupportedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
