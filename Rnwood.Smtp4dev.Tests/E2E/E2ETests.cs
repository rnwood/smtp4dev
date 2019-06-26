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

				HomePage.MessageListControl messageList = WaitFor(() => homePage.MessageList);
				Assert.NotNull(messageList);

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

				HomePage.Grid.GridRow messageRow = WaitFor(() => messageList.Grid?.Rows?.SingleOrDefault());
				Assert.NotNull(messageRow);

				Assert.Contains(messageRow.Cells, c => c.Text.Contains(messageSubject));
			});
		}

		private T WaitFor<T>(Func<T> findElement) where T : class
		{
			T result = null;

			CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

			while (result == null && !timeout.IsCancellationRequested)
			{
				result = findElement();
				Thread.Sleep(100);
			}

			Assert.NotNull(result);

			return result;
		}

		private void RunE2ETest2(Action<IWebDriver, Uri, int> test)
		{

		}

		private void RunE2ETest(Action<IWebDriver, Uri, int> test)
		{
			string outputPath = new DirectoryInfo("../../../../Rnwood.Smtp4dev/bin/").EnumerateDirectories().First().EnumerateDirectories().First().FullName;

			string mainModule = Path.Combine(outputPath, "Rnwood.Smtp4dev.dll");

			CancellationToken startupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token;

			Thread captureOutputThread = null;
			using (ProcessHost serverProcess = new ProcessHost("cmd", Path.GetFullPath("../../../../Rnwood.Smtp4dev/")))
			{
				serverProcess.StartAsChild($"/c dotnet \"{mainModule}\" --urls=\"http://*:0\" --smtpport=0 --db=\"\" 2>&1");

				try
				{
					Uri baseUrl = null;
					int? smtpPortNumber = 0;

					List<string> outputLines = new List<string>();

					while (serverProcess.IsAlive() && (baseUrl == null || !smtpPortNumber.HasValue))
					{

						string newLines = serverProcess.StdOut.ReadAllWithTimeout(Encoding.UTF8, TimeSpan.FromSeconds(15));
						startupTimeout.ThrowIfCancellationRequested();
						output.WriteLine(newLines);
						outputLines.AddRange(newLines.Split("\n"));

						string nowRunningOutput = outputLines.FirstOrDefault(o => o.StartsWith("Now listening on: http://"));
						string smtpServerRunningOutput = outputLines.FirstOrDefault(o => o.StartsWith("SMTP Server is listening on port"));

						if (!string.IsNullOrEmpty(nowRunningOutput) && !string.IsNullOrEmpty(smtpServerRunningOutput))
						{
							int portNumber = int.Parse(Regex.Replace(nowRunningOutput, @".*http://[^\s]+:(\d+)", "$1"));
							baseUrl = new Uri("http://localhost:" + portNumber);
							smtpPortNumber = int.Parse(Regex.Replace(smtpServerRunningOutput, @"SMTP Server is listening on port (\d+).*", "$1"));
							break;
						}
					}

					Assert.True(serverProcess.IsAlive(), "Server process failed");

					captureOutputThread = new Thread((s) =>
					{
						do
						{
							string newStdOut = serverProcess.StdOut.ReadAllText(Encoding.UTF8);
							if (!string.IsNullOrEmpty(newStdOut))
							{
								output.WriteLine(newStdOut);
							}

							string newStdErr = serverProcess.StdErr.ReadAllText(Encoding.UTF8);
							if (!string.IsNullOrEmpty(newStdErr))
							{
								output.WriteLine(newStdErr);
							}
							Thread.Sleep(100);
						} while (serverProcess.IsAlive());

					});
					captureOutputThread.Start();




					ChromeOptions chromeOptions = new ChromeOptions();
					if (!Debugger.IsAttached)
					{
						chromeOptions.AddArgument("--headless");
					}

					using (IWebDriver browser = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), chromeOptions))
					{
						try
						{
							test(browser, baseUrl, smtpPortNumber.Value);

						}
						finally
						{
							browser.Quit();
						}
					}

				}
				finally
				{
					if (serverProcess.IsAlive())
					{
						serverProcess.Kill();
					}

					if (captureOutputThread != null)
					{
						if (!captureOutputThread.Join(TimeSpan.FromSeconds(10)))
						{
							captureOutputThread.Abort();
						}
					
					}
				}
			}
		}

	}
}

