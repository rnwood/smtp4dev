using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.PageObjects;
using Rnwood.Smtp4dev.Tests.E2E.PageModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests.Client
{
	public class E2ETests
	{
		private ITestOutputHelper output;

		public E2ETests(ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public async Task Blah()
		{
			string mainModule = Path.GetFullPath("../../../../Rnwood.Smtp4dev/bin/Debug/netcoreapp2.2/Rnwood.Smtp4dev.dll");

			Process process = Process.Start(new ProcessStartInfo("cmd", $"/k dotnet \"{mainModule}\" --database=\"\" 2>&1")
			{
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				WorkingDirectory = Path.GetFullPath( "../../../../Rnwood.Smtp4dev")
			}) ;

			process.OutputDataReceived += (s, ea) => output.WriteLine(ea.Data);
			process.BeginOutputReadLine();

			process.Exited += (s,ea ) => throw new Exception($"Server process has exited prematurely with exit code {process.ExitCode}");
			if (process.HasExited)
			{
				throw new Exception($"Server process has exited prematurely with exit code {process.ExitCode}");
			}

			try
			{
				using (IWebDriver browser = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
				{
					browser.Navigate().GoToUrl("http://localhost:1234");


					HomePage homePage = new HomePage(browser);
				}
			}
			finally
			{
				process.Kill();
			}
		}


	}
}
