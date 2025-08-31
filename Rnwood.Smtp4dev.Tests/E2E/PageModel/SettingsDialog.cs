using OpenQA.Selenium;
using System.Threading;

namespace Rnwood.Smtp4dev.Tests.E2E.PageModel
{
    public class SettingsDialog
    {
        private IWebDriver browser;

        public SettingsDialog(IWebDriver browser)
        {
            this.browser = browser;
        }

        public IWebElement DisableMessageSanitisationSwitch 
        {
            get
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
                            return browser.FindElement(By.XPath(selector));
                        }
                        catch (NoSuchElementException)
                        {
                            continue;
                        }
                    }
                    throw new NoSuchElementException("Could not find sanitization switch with any selector");
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }

        public IWebElement SaveButton => browser.FindElement(By.XPath("//span[text()='OK']/.."));

        public void ToggleDisableMessageSanitisation()
        {
            var switchElement = DisableMessageSanitisationSwitch;
            if (switchElement == null)
            {
                throw new NoSuchElementException("Could not find the sanitization switch element to toggle");
            }
            switchElement.Click();
        }

        public void Save()
        {
            try
            {
                SaveButton.Click();
            }
            catch (NoSuchElementException ex)
            {
                throw new NoSuchElementException("Could not find the Save/OK button in settings dialog", ex);
            }
        }

        public bool IsVisible()
        {
            try
            {
                var dialog = browser.FindElement(By.XPath("//div[contains(@class, 'el-dialog')]//span[text()='Settings']/.."));
                return dialog.Displayed;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        public void WaitUntilVisible()
        {
            var timeout = new CancellationTokenSource(System.TimeSpan.FromSeconds(10));
            while (!IsVisible() && !timeout.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }
        }

        public void WaitUntilClosed()
        {
            var timeout = new CancellationTokenSource(System.TimeSpan.FromSeconds(10));
            while (IsVisible() && !timeout.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }
        }
    }
}