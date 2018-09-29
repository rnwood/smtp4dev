using System;
using MimeKit;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class MimeEntityVisitor
    {
        public static T Visit<T>(MimeEntity entity, T parentResult, Func<MimeEntity, T, T> action)
        {
            var result = action(entity, parentResult);

            if (entity is Multipart multipart)
                foreach (var childEntity in multipart)
                    Visit(childEntity, result, action);
            else if (entity is MessagePart rfc822) Visit(rfc822.Message.Body, result, action);

            return result;
        }
    }
}