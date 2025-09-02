using Microsoft.Playwright;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Tests.E2E.PageModel
{
    public class SettingsDialog
    {
        private IPage page;

        public SettingsDialog(IPage page)
        {
            this.page = page;
        }

        public async Task<IElementHandle> GetDisableMessageSanitisationSwitchAsync()
        {
            try
            {
                // Try multiple selectors for Element Plus switch
                var selectors = new[]
                {
                    "//label[contains(text(), 'Disable HTML message sanitisation')]/following-sibling::div//div[contains(@class, 'el-switch')]",
                    "//label[contains(text(), 'Disable HTML message sanitisation')]/following-sibling::div//input",
                    "//div[contains(@class, 'el-form-item')]//label[contains(text(), 'Disable HTML message sanitisation')]/following-sibling::*//*[contains(@class, 'el-switch')]",
                    "//div[contains(@class, 'el-form-item')]//label[contains(text(), 'Disable HTML message sanitisation')]/..//*[contains(@class, 'el-switch')]"
                };

                foreach (var selector in selectors)
                {
                    try
                    {
                        var element = await page.QuerySelectorAsync(selector);
                        if (element != null) return element;
                    }
                    catch
                    {
                        continue;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IElementHandle> GetSaveButtonAsync()
        {
            return await page.QuerySelectorAsync("//span[text()='OK']/..");
        }

        public async Task ToggleDisableMessageSanitisationAsync()
        {
            var switchElement = await GetDisableMessageSanitisationSwitchAsync();
            if (switchElement == null)
            {
                throw new System.InvalidOperationException("Could not find the sanitization switch element to toggle");
            }
            await switchElement.ClickAsync();
        }

        public async Task SaveAsync()
        {
            var saveButton = await GetSaveButtonAsync();
            if (saveButton == null)
            {
                throw new System.InvalidOperationException("Could not find the Save/OK button in settings dialog");
            }
            await saveButton.ClickAsync();
        }

        public async Task<bool> IsVisibleAsync()
        {
            try
            {
                var dialog = await page.QuerySelectorAsync("//div[contains(@class, 'el-dialog')]//span[text()='Settings']/..");
                return dialog != null && await dialog.IsVisibleAsync();
            }
            catch
            {
                return false;
            }
        }

        public async Task WaitUntilVisibleAsync()
        {
            try
            {
                await page.WaitForSelectorAsync("//div[contains(@class, 'el-dialog')]//span[text()='Settings']/..", new PageWaitForSelectorOptions { Timeout = 10000 });
            }
            catch
            {
                // Dialog didn't appear within timeout
            }
        }

        public async Task WaitUntilClosedAsync()
        {
            try
            {
                await page.WaitForSelectorAsync("//div[contains(@class, 'el-dialog')]//span[text()='Settings']/..", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached, Timeout = 10000 });
            }
            catch
            {
                // Dialog didn't close within timeout
            }
        }
    }
}