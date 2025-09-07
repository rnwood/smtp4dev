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

        public ILocator GetDarkModeToggleButton()
        {
            // The dark mode toggle is the first circular button in the header
            // Since it's the only icon-only circular button in the header after the logo
            return page.Locator("header button.el-button.is-circle").First;
        }

        public async Task ToggleDarkModeAsync()
        {
            var darkModeButton = GetDarkModeToggleButton();
            await darkModeButton.ClickAsync();
        }

        public async Task<bool> IsDarkModeActiveAsync()
        {
            // Check if the html element has the 'dark' class
            var htmlElement = page.Locator("html");
            var classes = await htmlElement.GetAttributeAsync("class");
            return classes?.Contains("dark") == true;
        }

        public async Task SetDarkModeAsync(bool isDarkMode)
        {
            bool currentDarkMode = await IsDarkModeActiveAsync();
            if (currentDarkMode != isDarkMode)
            {
                await ToggleDarkModeAsync();
                await page.WaitForTimeoutAsync(1000); // Allow UI to update
            }
        }

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
            }
        }
    }
}