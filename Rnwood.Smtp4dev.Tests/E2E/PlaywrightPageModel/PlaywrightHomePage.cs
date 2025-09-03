using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Tests.E2E.PlaywrightPageModel
{
    public class PlaywrightHomePage
    {
        private readonly IPage page;

        public PlaywrightHomePage(IPage page)
        {
            this.page = page;
        }

        public async Task<PlaywrightMessageListControl> GetMessageListAsync()
        {
            var messageListElement = page.Locator(".messagelist").First;
            await messageListElement.WaitForAsync();
            return new PlaywrightMessageListControl(messageListElement);
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

        public PlaywrightSettingsDialog SettingsDialog => new PlaywrightSettingsDialog(page);

        public PlaywrightMessageView MessageView => new PlaywrightMessageView(page);

        public class PlaywrightMessageListControl
        {
            private readonly ILocator element;

            public PlaywrightMessageListControl(ILocator element)
            {
                this.element = element;
            }

            public PlaywrightGrid GetGrid()
            {
                var gridElement = element.Locator("table.el-table__body");
                return new PlaywrightGrid(gridElement);
            }
        }

        public class PlaywrightGrid
        {
            private readonly ILocator element;

            public PlaywrightGrid(ILocator element)
            {
                this.element = element;
            }

            public async Task<IReadOnlyList<PlaywrightGridRow>> GetRowsAsync()
            {
                var rowElements = await element.Locator("tr").AllAsync();
                return rowElements.Select(e => new PlaywrightGridRow(e)).ToList();
            }

            public class PlaywrightGridRow
            {
                private readonly ILocator element;

                public PlaywrightGridRow(ILocator element)
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
            }
        }
    }
}