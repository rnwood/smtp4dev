using Microsoft.Playwright;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Tests.E2E.PageModel
{
    public class MessageView
    {
        private IPage page;

        public MessageView(IPage page)
        {
            this.page = page;
        }

        public async Task<IElementHandle> GetViewTabAsync()
        {
            try
            {
                // Element Plus tabs create tab headers with specific classes
                var element = await page.QuerySelectorAsync("//div[contains(@class, 'el-tab-pane') and @id='view']//ancestor::div[contains(@class, 'el-tabs')]//div[contains(@class, 'el-tabs__item') and contains(text(), 'View')]");
                if (element != null) return element;
                
                // Try alternative selector - look for tab with View text
                return await page.QuerySelectorAsync("//div[contains(@class, 'el-tabs__item') and contains(., 'View')]");
            }
            catch
            {
                return null;
            }
        }
        
        public async Task<IElementHandle> GetHtmlSubTabAsync()
        {
            try
            {
                // Look for HTML sub-tab within the inner tabs
                return await page.QuerySelectorAsync("//div[contains(@class, 'el-tabs__item') and contains(text(), 'HTML')]");
            }
            catch
            {
                return null;
            }
        }

        public async Task<IElementHandle> GetHtmlFrameAsync()
        {
            try
            {
                return await page.WaitForSelectorAsync("iframe.htmlview", new PageWaitForSelectorOptions { Timeout = 10000 });
            }
            catch
            {
                throw new System.TimeoutException("HTML iframe not found");
            }
        }

        public async Task<IElementHandle> GetSanitizationWarningAsync()
        {
            try
            {
                return await page.QuerySelectorAsync("//div[contains(@class, 'el-alert--warning')]//p[contains(text(), 'Message HTML was sanitized')]");
            }
            catch
            {
                return null;
            }
        }

        public async Task ClickViewTabAsync()
        {
            var viewTab = await GetViewTabAsync();
            if (viewTab != null)
            {
                await viewTab.ClickAsync();
            }
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
            await Task.Delay(500); // Wait for content to load
            await ClickHtmlSubTabAsync();
        }

        public async Task<string> GetHtmlFrameContentAsync()
        {
            var htmlFrame = await GetHtmlFrameAsync();
            if (htmlFrame == null) return "";
            
            // Switch to frame and get content
            var frame = await htmlFrame.ContentFrameAsync();
            if (frame == null) return "";
            
            var bodyElement = await frame.QuerySelectorAsync("body");
            if (bodyElement == null) return "";
            
            return await bodyElement.GetAttributeAsync("innerHTML") ?? "";
        }

        public async Task<bool> IsSanitizationWarningVisibleAsync()
        {
            var warning = await GetSanitizationWarningAsync();
            return warning != null && await warning.IsVisibleAsync();
        }

        public async Task WaitForHtmlFrameAsync()
        {
            try
            {
                await page.WaitForSelectorAsync("iframe.htmlview", new PageWaitForSelectorOptions { Timeout = 10000 });
            }
            catch
            {
                throw new System.TimeoutException("HTML frame did not appear within timeout");
            }
        }
    }
}