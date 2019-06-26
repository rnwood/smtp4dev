using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using MimeKit;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.PageObjects;
using Rnwood.Smtp4dev.Tests.E2E.PageModel;
using RunProcess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.E2E
{
	public class E2ETests
	{
		private ITestOutputHelper output;

		public E2ETests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void CheckMessageIsReceivedAndDisplayed()
		{
			RunE2ETest((browser, baseUrl, smtpPortNumber) =>
			{

				browser.Navigate().GoToUrl(baseUrl);
				HomePage homePage = new HomePage(browser);

				string messageSubject = Guid.NewGuid().ToString();
				using (SmtpClient smtpClient = new SmtpClient())
				{
					MimeMessage message = new MimeMessage();
					message.To.Add(new MailboxAddress("to@to.com"));
					message.From.Add(new MailboxAddress("from@from.com"));

					message.Subject = messageSubject;
					message.Body = new TextPart()
					{
						Text = "Body of end to end test"
					};

					smtpClient.Connect("localhost", smtpPortNumber, false, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
					smtpClient.Send(message);
					smtpClient.Disconnect(true, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
				}

				var messageRow = homePage.MessageList?.Grid?.Rows?.SingleOrDefault();
				Assert.NotNull(messageRow);

				Assert.Contains(messageRow.Cells, c => c.Text.Contains(messageSubject));
			});
		}


		private void RunE2ETest(Action<IWebDriver, Uri, int> test)
		{
			string mainModule = Path.GetFullPath("../../../../Rnwood.Smtp4dev/bin/Debug/netcoreapp2.2/Rnwood.Smtp4dev.dll");

			using (ProcessHost serverProcess = new ProcessHost("cmd", Path.GetFullPath("../../../../Rnwood.Smtp4dev/")))
			{
				serverProcess.StartAsChild($"/k dotnet \"{mainModule}\" --urls=\"http://*:0\" --smtpport=0 --db=\"\" 2>&1");

				try
				{

					string outputLines = serverProcess.StdOut.ReadAllWithTimeout(Encoding.UTF8, TimeSpan.FromSeconds(15));
					output.WriteLine(outputLines);
					Assert.True(serverProcess.IsAlive(), "Server process failed");

					string nowRunningOutput = outputLines.Split("\n").FirstOrDefault(o => o.StartsWith("Now listening on: http://"));
					Assert.NotEmpty(nowRunningOutput);

					int portNumber = int.Parse(Regex.Replace(nowRunningOutput, @".*http://[^\s]+:(\d+)", "$1"));
					Uri baseUrl = new Uri("http://localhost:" + portNumber);

					string smtpServerRunningOutput = outputLines.Split("\n").FirstOrDefault(o => o.StartsWith("SMTP Server is listening on port"));
					Assert.NotEmpty(smtpServerRunningOutput);

					int smtpPortNumber = int.Parse(Regex.Replace(smtpServerRunningOutput, @"SMTP Server is listening on port (\d+).*", "$1"));

					using (IWebDriver browser = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
					{
						try
						{
							test(browser, baseUrl, smtpPortNumber);

						}
						finally
						{
							browser.Quit();
						}
					}

				}
				finally
				{
					serverProcess.Kill();
					output.WriteLine(serverProcess.StdOut.ReadAllText(Encoding.UTF8));
				}
			}



		}
	}


}

