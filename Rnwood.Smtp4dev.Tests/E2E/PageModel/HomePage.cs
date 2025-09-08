using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Tests.E2E.PageModel
{
    public class HomePage
    {
        private readonly IPage page;

        public HomePage(IPage page)
        {
            this.page = page;
        }

        public async Task<MessageListControl> GetMessageListAsync()
        {
            var messageListElement = page.Locator(".messagelist").First;
            await messageListElement.WaitForAsync();
            return new MessageListControl(messageListElement);
        }

        public ILocator GetSettingsButton()
        {
            return page.Locator("button[title='Settings']");
        }

        public async Task OpenSettingsAsync()
        {
            var settingsButton = GetSettingsButton();
            await settingsButton.ClickAsync();
        }

        public SettingsDialog SettingsDialog => new SettingsDialog(page);

        public MessageView MessageView => new MessageView(page);

        public class MessageListControl
        {
            private readonly ILocator element;

            public MessageListControl(ILocator element)
            {
                this.element = element;
            }

            public Grid GetGrid()
            {
                var gridElement = element.Locator("table.el-table__body");
                return new Grid(gridElement);
            }
        }

        public class Grid
        {
            private readonly ILocator element;

            public Grid(ILocator element)
            {
                this.element = element;
            }

            public async Task<IReadOnlyList<GridRow>> GetRowsAsync()
            {
                var rowElements = await element.Locator("tr").AllAsync();
                return rowElements.Select(e => new GridRow(e)).ToList();
            }

            public class GridRow
            {
                private readonly ILocator element;

                public GridRow(ILocator element)
                {
                    this.element = element;
                }

                public async Task<IReadOnlyList<ILocator>> GetCellsAsync()
                {
                    return await element.Locator("td div.cell").AllAsync();
                }

                public async Task ClickAsync()
                {
                    await element.ClickAsync();
                }

                public async Task<bool> ContainsTextAsync(string text)
                {
                    var cells = await GetCellsAsync();
                    foreach (var cell in cells)
                    {
                        var cellText = await cell.TextContentAsync();
                        if (cellText?.Contains(text) == true)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                public async Task<bool> IsSelectedAsync()
                {
                    var className = await element.GetAttributeAsync("class");
                    return className?.Contains("current-row") == true;
                }
            }
        }
    }
}