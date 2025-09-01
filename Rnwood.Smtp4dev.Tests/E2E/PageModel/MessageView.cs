using OpenQA.Selenium;
using System.Threading;

namespace Rnwood.Smtp4dev.Tests.E2E.PageModel
{
    public class MessageView
    {
        private IWebDriver browser;

        public MessageView(IWebDriver browser)
        {
            this.browser = browser;
        }

        public IWebElement ViewTab 
        {
            get
            {
                try
                {
                    // Element Plus tabs create tab headers with specific classes
                    return browser.FindElement(By.XPath("//div[contains(@class, 'el-tab-pane') and @id='view']//ancestor::div[contains(@class, 'el-tabs')]//div[contains(@class, 'el-tabs__item') and contains(text(), 'View')]"));
                }
                catch (NoSuchElementException)
                {
                    // Try alternative selector - look for tab with View text
                    return browser.FindElement(By.XPath("//div[contains(@class, 'el-tabs__item') and contains(., 'View')]"));
                }
            }
        }
        
        public IWebElement HtmlSubTab 
        {
            get
            {
                try
                {
                    // Look for HTML sub-tab within the inner tabs
                    return browser.FindElement(By.XPath("//div[contains(@class, 'el-tabs__item') and contains(text(), 'HTML')]"));
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }

        public IWebElement HtmlFrame
        {
            get
            {
                var timeout = new CancellationTokenSource(System.TimeSpan.FromSeconds(10));
                while (!timeout.IsCancellationRequested)
                {
                    try
                    {
                        return browser.FindElement(By.CssSelector("iframe.htmlview"));
                    }
                    catch (NoSuchElementException)
                    {
                        Thread.Sleep(100);
                    }
                }
                throw new NoSuchElementException("HTML iframe not found");
            }
        }

        public IWebElement SanitizationWarning
        {
            get
            {
                try
                {
                    return browser.FindElement(By.XPath("//div[contains(@class, 'el-alert--warning')]//p[contains(text(), 'Message HTML was sanitized')]"));
                }
                catch (NoSuchElementException)
                {
                    return null;
                }
            }
        }

        public void ClickViewTab()
        {
            ViewTab.Click();
        }
        
        public void ClickHtmlSubTab()
        {
            if (HtmlSubTab != null)
            {
                HtmlSubTab.Click();
            }
        }

        public void ClickHtmlTab()
        {
            // First click on the View tab, then on HTML sub-tab if it exists
            ClickViewTab();
            Thread.Sleep(500); // Wait for content to load
            ClickHtmlSubTab();
        }

        public string GetHtmlFrameContent()
        {
            browser.SwitchTo().Frame(HtmlFrame);
            var content = browser.FindElement(By.TagName("body")).GetAttribute("innerHTML");
            browser.SwitchTo().DefaultContent();
            return content;
        }

        public bool IsSanitizationWarningVisible()
        {
            return SanitizationWarning != null && SanitizationWarning.Displayed;
        }

        public void WaitForHtmlFrame()
        {
            var timeout = new CancellationTokenSource(System.TimeSpan.FromSeconds(10));
            while (!timeout.IsCancellationRequested)
            {
                try
                {
                    var frame = HtmlFrame;
                    if (frame.Displayed)
                        return;
                }
                catch (NoSuchElementException)
                {
                    // Frame not found yet, continue waiting
                }
                Thread.Sleep(100);
            }
            throw new System.TimeoutException("HTML frame did not appear within timeout");
        }
    }
}