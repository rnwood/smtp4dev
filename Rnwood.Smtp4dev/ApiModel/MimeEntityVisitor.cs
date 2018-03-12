using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class MimeEntityVisitor
    {

        public static T Visit<T>(MimeEntity entity, T parentResult, Func<MimeEntity, T, T> action)
        {
            T result = action(entity, parentResult);

            if (entity is Multipart multipart)
            {
                foreach (MimeEntity childEntity in multipart)
                {
                    Visit(childEntity, result, action);
                }
            }
            else if (entity is MimeKit.MessagePart rfc822)
            {
                Visit(rfc822.Message.Body, result, action);
            }

            return result;
        }
    }
}
