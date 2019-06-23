using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Tests.E2E.PageModel
{
	public class HomePage
	{

		public MessageListControl MessageList => new MessageListControl(browser.FindElement(By.ClassName("messagelist")));

		private IWebDriver browser;

		public HomePage(IWebDriver browser)
		{
			this.browser = browser;
		}

		public class MessageListControl
		{
			public IWebElement ClearButton => webElement.FindElement(By.TagName("button"));

			private IWebElement webElement;

			public MessageListControl(IWebElement webElement)
			{
				this.webElement = webElement;
			}
		}
	}
}
