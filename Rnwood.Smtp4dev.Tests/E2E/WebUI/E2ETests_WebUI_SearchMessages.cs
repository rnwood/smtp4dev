using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Rnwood.Smtp4dev.Tests.E2E.WebUI.PageModel;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E.WebUI
{
    [Collection("E2ETests")]
    public class E2ETests_WebUI_SearchMessages : E2ETestsWebUIBase
    {
        public E2ETests_WebUI_SearchMessages(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void SearchResetsPagination()
        {
            RunUITestAsync(nameof(SearchResetsPagination), async (page, baseUrl, smtpPortNumber) =>
            {
                await page.GotoAsync(baseUrl.ToString());
                var homePage = new HomePage(page);
                var messageList = await WaitForAsync(async () => await homePage.GetMessageListAsync());

                string searchSubject = Guid.NewGuid().ToString();
                string otherSubject = Guid.NewGuid().ToString();
                await HomePage.SendTestEmailAsync(smtpPortNumber, searchSubject);
                await HomePage.SendTestEmailAsync(smtpPortNumber, otherSubject);

                var grid = messageList.GetGrid();
                await WaitForAsync(async () => (await grid.GetRowsAsync()).FirstOrDefault());

                var pageSizeInput = page.Locator(".messagelist input[placeholder='Page size']");
                await pageSizeInput.FillAsync("1");
                await pageSizeInput.PressAsync("Enter");
                await page.Locator(".messagelist button.btn-next").ClickAsync();
                var otherMessageRow = page.Locator(".messagelist table.el-table__body tr").Filter(new() { HasText = otherSubject });
                await otherMessageRow.WaitForAsync();

                await page.Locator(".messagelist input[placeholder='Search']").FillAsync(searchSubject);
                var matchingRow = page.Locator(".messagelist table.el-table__body tr").Filter(new() { HasText = searchSubject });
                await matchingRow.WaitForAsync();
                var filteredOutRow = page.Locator(".messagelist table.el-table__body tr").Filter(new() { HasText = otherSubject });
                await filteredOutRow.WaitForAsync(new() { State = WaitForSelectorState.Detached });
                Assert.Equal(1, await matchingRow.CountAsync());
                Assert.Equal(0, await filteredOutRow.CountAsync());
            }, new UITestOptions
            {
                InMemoryDB = true
            });
        }
    }
}
