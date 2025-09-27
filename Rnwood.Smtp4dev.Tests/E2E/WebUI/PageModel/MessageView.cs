using Microsoft.Playwright;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel
{
    public class MessageView
    {
        private readonly IPage page;

        public MessageView(IPage page)
        {
            this.page = page;
        }

        public async Task<ILocator> GetViewTabAsync()
        { 
            // Fallback selector
            return page.Locator("div[class*='el-tabs__item']:text-is('View')");
        }

        public async Task<ILocator> GetHtmlSubTabAsync()
        {
            try
            {
                var htmlTab = page.Locator("div[class*='el-tabs__item']:has-text('HTML')");
                await htmlTab.WaitForAsync(new LocatorWaitForOptions { Timeout = 1000 });
                return htmlTab;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ILocator> GetHtmlFrameAsync()
        {
            await page.WaitForSelectorAsync("iframe.htmlview", new PageWaitForSelectorOptions { Timeout = 10000 });
            return page.Locator("iframe.htmlview");
        }

        public async Task<ILocator> GetSanitizationWarningAsync()
        {
            try
            {
                var warning = page.Locator("div[class*='el-alert--warning'] p:has-text('Message HTML was sanitized')");
                await warning.WaitForAsync(new LocatorWaitForOptions { Timeout = 1000 });
                return warning;
            }
            catch
            {
                return null;
            }
        }

        public async Task ClickViewTabAsync()
        {
            var viewTab = await GetViewTabAsync();
            await viewTab.ClickAsync();
        }

        public async Task ClickHtmlSubTabAsync()
        {
            var htmlSubTab = await GetHtmlSubTabAsync();
            if (htmlSubTab != null)
            {
                await htmlSubTab.ClickAsync();
            }
        }

        public async Task ClickHtmlTabAsync()
        {
            // First click on the View tab, then on HTML sub-tab if it exists
            await ClickViewTabAsync();
            await page.WaitForTimeoutAsync(500); // Wait for content to load
            await ClickHtmlSubTabAsync();
        }

        public async Task<string> GetHtmlFrameContentAsync()
        {
            var frame = await GetHtmlFrameAsync();
            var frameHandle = await frame.ElementHandleAsync();

            if (frameHandle != null)
            {
                var frameContent = await frameHandle.ContentFrameAsync();
                if (frameContent != null)
                {
                    var body = frameContent.Locator("body");
                    return await body.InnerHTMLAsync();
                }
            }

            return "";
        }

        public async Task<bool> IsSanitizationWarningVisibleAsync()
        {
            var warning = await GetSanitizationWarningAsync();
            return warning != null && await warning.IsVisibleAsync();
        }

        public async Task WaitForHtmlFrameAsync()
        {
            await page.WaitForSelectorAsync("iframe.htmlview", new PageWaitForSelectorOptions { Timeout = 10000 });
            var frame = page.Locator("iframe.htmlview");
            await frame.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
        }
    }
}