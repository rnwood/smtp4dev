using Microsoft.Playwright;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel
{
    public class SettingsDialog
    {
        private readonly IPage page;

        public SettingsDialog(IPage page)
        {
            this.page = page;
        }

        public async Task<ILocator> GetDisableMessageSanitisationSwitchAsync()
        {
            try
            {
                // Try multiple selectors for Element Plus switch
                var selectors = new[]
                {
                    "label:has-text('Disable HTML message sanitisation') ~ div .el-switch",
                    "label:has-text('Disable HTML message sanitisation') ~ div input",
                    ".el-form-item:has(label:has-text('Disable HTML message sanitisation')) .el-switch",
                    ".el-form-item:has(label:has-text('Disable HTML message sanitisation')) .el-switch input"
                };

                foreach (var selector in selectors)
                {
                    try
                    {
                        var element = page.Locator(selector);
                        await element.WaitForAsync(new LocatorWaitForOptions { Timeout = 1000 });
                        return element;
                    }
                    catch
                    {
                        continue;
                    }
                }
                
                throw new PlaywrightException("Could not find sanitization switch with any selector");
            }
            catch
            {
                return null;
            }
        }

        public ILocator GetSaveButton()
        {
            return page.Locator("span:has-text('OK')").Locator("..");
        }

        public async Task ToggleDisableMessageSanitisationAsync()
        {
            var switchElement = await GetDisableMessageSanitisationSwitchAsync();
            if (switchElement == null)
            {
                throw new PlaywrightException("Could not find the sanitization switch element to toggle");
            }
            await switchElement.ClickAsync();
        }

        public async Task SaveAsync()
        {
            try
            {
                var saveButton = GetSaveButton();
                await saveButton.ClickAsync();
            }
            catch
            {
                throw new PlaywrightException("Could not find the Save/OK button in settings dialog");
            }
        }

        public async Task<bool> IsVisibleAsync()
        {
            try
            {
                var dialog = page.Locator("div[class*='el-dialog']:has(span:has-text('Settings'))");
                return await dialog.IsVisibleAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task WaitUntilVisibleAsync()
        {
            await page.WaitForSelectorAsync("div[class*='el-dialog']:has(span:has-text('Settings'))", 
                new PageWaitForSelectorOptions { Timeout = 10000 });
        }

        public async Task WaitUntilClosedAsync()
        {
            await page.WaitForSelectorAsync("div[class*='el-dialog']:has(span:has-text('Settings'))", 
                new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden, Timeout = 10000 });
        }
    }
}