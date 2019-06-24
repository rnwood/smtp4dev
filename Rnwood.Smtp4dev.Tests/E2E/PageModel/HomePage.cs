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
			public Grid Grid => new Grid(webElement.FindElement(By.CssSelector("table.el-table__body")));

			private IWebElement webElement;

			public MessageListControl(IWebElement webElement)
			{
				this.webElement = webElement;
			}
		}

		public class Grid
		{
			private IWebElement webElement;

			public Grid(IWebElement webElement)
			{
				this.webElement = webElement;
			}

			public IReadOnlyCollection<GridRow> Rows => webElement.FindElements(By.TagName("tr")).Select(e => new GridRow(e)).ToList();


			public class GridRow
			{
				private IWebElement webElement;

				public GridRow(IWebElement webElement)
				{
					this.webElement = webElement;
				}

				public IReadOnlyCollection<IWebElement> Cells => webElement.FindElements(By.CssSelector("td div.cell"));
			}
		}
	}
}
