using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev
{
    public class HtmlBodyInputFormatter : IInputFormatter
    {
        public bool CanRead(InputFormatterContext context)
        {
            return context.HttpContext.Request.ContentType == "text/html";
        }

        public async Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            string value = await context.ReaderFactory(context.HttpContext.Request.Body, System.Text.Encoding.UTF8).ReadToEndAsync();
            return InputFormatterResult.Success(value);
        }
    }
}
