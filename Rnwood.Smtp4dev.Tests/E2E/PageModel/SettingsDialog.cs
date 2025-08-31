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

        public IWebElement DisableMessageSanitisationSwitch => browser.FindElement(By.XPath("//label[contains(text(), 'Disable HTML message sanitisation')]/following-sibling::div//input[@type='checkbox']"));

        public IWebElement SaveButton => browser.FindElement(By.XPath("//button/span[text()='OK']/.."));

        public void ToggleDisableMessageSanitisation()
        {
            DisableMessageSanitisationSwitch.Click();
        }

        public void Save()
        {
            SaveButton.Click();
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