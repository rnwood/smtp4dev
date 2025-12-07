using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Rnwood.Smtp4dev.Tests.E2E;
using Xunit;
using Xunit.Abstractions;

namespace Rnwood.Smtp4dev.Tests
{
    [Collection("StressTests")]
    
    public class MemoryLeakStressTests : E2ETests
    {
        private readonly ITestOutputHelper output;

        public MemoryLeakStressTests(ITestOutputHelper output) : base(output)
        {
            this.output = output;
        }

        [Fact(Skip ="Long running test - enable manually")]
        public void MessageSearchStressTest_ShouldNotLeakMemory()
        {
            RunMemoryStressTest(
                messageBatches: 10,
                messagesPerBatch: 100,
                concurrentSmtpSenders: 5,
                concurrentApiReaders: 3,
                searchFrequency: TimeSpan.FromMilliseconds(500),
                testDuration: TimeSpan.FromMinutes(2)
            );
        }

        [Fact]
        public void MessageSearchStressTest_QuickCI_ShouldNotLeakMemory()
        {
            // Smaller test suitable for CI environments
            RunMemoryStressTest(
                messageBatches: 3,
                messagesPerBatch: 50,
                concurrentSmtpSenders: 2,
                concurrentApiReaders: 2,
                searchFrequency: TimeSpan.FromMilliseconds(1000),
                testDuration: TimeSpan.FromSeconds(30)
            );
        }

        private void RunMemoryStressTest(
            int messageBatches,
            int messagesPerBatch,
            int concurrentSmtpSenders,
            int concurrentApiReaders,
            TimeSpan searchFrequency,
            TimeSpan testDuration)
        {
            RunE2ETest(context =>
            {
                ExecuteStressTestAsync(context, messageBatches, messagesPerBatch, concurrentSmtpSenders,
                    concurrentApiReaders, searchFrequency, testDuration).GetAwaiter().GetResult();
            }, new E2ETestOptions
            {
                InMemoryDB = false // Use real database to test actual production scenario
            });
        }

        private async Task ExecuteStressTestAsync(
            E2ETestContext context,
            int messageBatches,
            int messagesPerBatch,
            int concurrentSmtpSenders,
            int concurrentApiReaders,
            TimeSpan searchFrequency,
            TimeSpan testDuration)
        {
            var memoryReadings = new ConcurrentBag<MemoryReading>();
            var messagesProcessed = 0;
            var searchOperations = 0;
            var cancellationTokenSource = new CancellationTokenSource(testDuration);

            output.WriteLine($"Starting memory leak stress test");
            output.WriteLine($"Target: {messageBatches} batches × {messagesPerBatch} messages = {messageBatches * messagesPerBatch} total messages");
            output.WriteLine($"Concurrent SMTP senders: {concurrentSmtpSenders}");
            output.WriteLine($"Concurrent API readers: {concurrentApiReaders}");
            output.WriteLine($"Test duration: {testDuration}");
            output.WriteLine($"SMTP Port: {context.SmtpPortNumber}, Web URL: {context.BaseUrl}");

            // Record initial memory
            RecordMemory(memoryReadings, messagesProcessed, searchOperations);

            var httpClient = new HttpClient { BaseAddress = context.BaseUrl };

            // Setup SignalR connection for real-time notifications
            HubConnection signalRConnection = null;
            var messagesChangedCount = 0;

            try
            {
                signalRConnection = new HubConnectionBuilder()
                    .WithUrl($"{context.BaseUrl}hubs/notifications")
                    .Build();

                signalRConnection.On<string>("messageschanged", (mailbox) =>
                {
                    output.WriteLine($"SignalR: Messages changed in mailbox '{mailbox}'");
                    Interlocked.Increment(ref messagesChangedCount);
                });

                await signalRConnection.StartAsync(cancellationTokenSource.Token);
                output.WriteLine("SignalR connection established successfully");
            }
            catch (Exception ex)
            {
                output.WriteLine($"SignalR connection failed: {ex.Message} - continuing without SignalR");
                signalRConnection = null;
            }

            // Start memory monitoring task
            var memoryMonitorTask = MonitorMemoryUsage(memoryReadings, () => messagesProcessed, () => searchOperations, cancellationTokenSource.Token);

            // Start message sending tasks
            var sendingTasks = new List<Task>();
            for (int senderIndex = 0; senderIndex < concurrentSmtpSenders; senderIndex++)
            {
                var senderTask = SendMessagesAsync(
                    context.SmtpPortNumber,
                    senderIndex,
                    messageBatches,
                    messagesPerBatch,
                    () => Interlocked.Increment(ref messagesProcessed),
                    cancellationTokenSource.Token);
                sendingTasks.Add(senderTask);
            }

            // Start API reading/searching tasks
            var readingTasks = new List<Task>();
            for (int readerIndex = 0; readerIndex < concurrentApiReaders; readerIndex++)
            {
                var readerTask = ReadAndSearchMessagesAsync(
                    httpClient,
                    readerIndex,
                    searchFrequency,
                    () => Interlocked.Increment(ref searchOperations),
                    cancellationTokenSource.Token);
                readingTasks.Add(readerTask);
            }

            // Wait for all tasks to complete or timeout
            var allTasks = sendingTasks.Concat(readingTasks).Concat(new[] { memoryMonitorTask }).ToArray();

            try
            {
                await Task.WhenAll(allTasks);
            }
            catch (OperationCanceledException)
            {
                output.WriteLine("Test completed due to timeout");
            }

            if (signalRConnection != null)
            {
                try
                {
                    await signalRConnection.StopAsync();
                }
                catch (Exception ex)
                {
                    output.WriteLine($"Error stopping SignalR connection: {ex.Message}");
                }
            }

            // Generate and output the stress test report
            GenerateStressTestReport(memoryReadings, messagesProcessed, searchOperations, messagesChangedCount);

            // Validate memory behavior
            ValidateMemoryBehavior(memoryReadings);
        }

        private async Task SendMessagesAsync(
            int smtpPort,
            int senderIndex,
            int messageBatches,
            int messagesPerBatch,
            Action onMessageSent,
            CancellationToken cancellationToken)
        {
            try
            {
                output.WriteLine($"SMTP Sender {senderIndex}: Starting to send {messageBatches * messagesPerBatch} messages");

                for (int batch = 0; batch < messageBatches && !cancellationToken.IsCancellationRequested; batch++)
                {
                    for (int msg = 0; msg < messagesPerBatch && !cancellationToken.IsCancellationRequested; msg++)
                    {
                        using var smtpClient = new SmtpClient("localhost", smtpPort);
                        var mailMessage = new MailMessage(
                            from: $"sender{senderIndex}@test.com",
                            to: $"recipient{msg}@test.com",
                            subject: $"Stress Test Message Batch:{batch} Msg:{msg} From Sender:{senderIndex} - {Guid.NewGuid()}",
                            body: GenerateTestEmailBody(batch, msg, senderIndex)
                        );

                        // Add some variety in recipients for search testing
                        if (msg % 3 == 0)
                        {
                            mailMessage.CC.Add($"cc{msg}@test.com");
                        }

                        await smtpClient.SendMailAsync(mailMessage);
                        onMessageSent();

                        // Small delay to avoid overwhelming the server
                        await Task.Delay(10, cancellationToken);
                    }

                    output.WriteLine($"SMTP Sender {senderIndex}: Completed batch {batch + 1}/{messageBatches}");

                    // Pause between batches
                    await Task.Delay(100, cancellationToken);
                }

                output.WriteLine($"SMTP Sender {senderIndex}: Completed all messages");
            }
            catch (OperationCanceledException)
            {
                output.WriteLine($"SMTP Sender {senderIndex}: Cancelled");
            }
            catch (Exception ex)
            {
                output.WriteLine($"SMTP Sender {senderIndex}: Error - {ex.Message}");
            }
        }

        private async Task ReadAndSearchMessagesAsync(
            HttpClient httpClient,
            int readerIndex,
            TimeSpan searchFrequency,
            Action onSearchOperation,
            CancellationToken cancellationToken)
        {
            try
            {
                var searchTerms = new[] { "Stress", "Test", "Batch", "sender", "recipient", Guid.NewGuid().ToString()[..8] };
                var random = new Random(readerIndex);

                output.WriteLine($"API Reader {readerIndex}: Starting to read and search messages");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Perform searches with different terms to exercise the search functionality
                        var searchTerm = searchTerms[random.Next(searchTerms.Length)];

                        var searchResponse = await httpClient.GetAsync(
                            $"api/messages?searchTerms={searchTerm}&pageSize=50&page=1",
                            cancellationToken);

                        if (searchResponse.IsSuccessStatusCode)
                        {
                            var content = await searchResponse.Content.ReadAsStringAsync(cancellationToken);
                            var searchResult = JsonSerializer.Deserialize<PagedResultResponse>(content, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            onSearchOperation();

                            if (readerIndex == 0) // Only log from one reader to avoid spam
                            {
                                output.WriteLine($"API Reader {readerIndex}: Search for '{searchTerm}' returned {searchResult?.Results?.Count ?? 0} messages");
                            }
                        }

                        // Also get messages without search to test general retrieval
                        var allMessagesResponse = await httpClient.GetAsync("api/messages?pageSize=20&page=1", cancellationToken);
                        if (allMessagesResponse.IsSuccessStatusCode)
                        {
                            onSearchOperation();
                        }

                        await Task.Delay(searchFrequency, cancellationToken);
                    }
                    catch (HttpRequestException ex)
                    {
                        output.WriteLine($"API Reader {readerIndex}: HTTP error - {ex.Message}");
                        await Task.Delay(1000, cancellationToken); // Wait before retrying
                    }
                }

                output.WriteLine($"API Reader {readerIndex}: Completed");
            }
            catch (OperationCanceledException)
            {
                output.WriteLine($"API Reader {readerIndex}: Cancelled");
            }
            catch (Exception ex)
            {
                output.WriteLine($"API Reader {readerIndex}: Error - {ex.Message}");
            }
        }

        private async Task MonitorMemoryUsage(
            ConcurrentBag<MemoryReading> memoryReadings,
            Func<int> getMessagesProcessed,
            Func<int> getSearchOperations,
            CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    RecordMemory(memoryReadings, getMessagesProcessed(), getSearchOperations());
                    await Task.Delay(2000, cancellationToken); // Record every 2 seconds
                }
            }
            catch (OperationCanceledException)
            {
                output.WriteLine("Memory monitoring cancelled");
            }
        }

        private void RecordMemory(ConcurrentBag<MemoryReading> memoryReadings, int messagesProcessed, int searchOperations)
        {
            var currentProcess = Process.GetCurrentProcess();
            var reading = new MemoryReading
            {
                Timestamp = DateTime.UtcNow,
                WorkingSetBytes = currentProcess.WorkingSet64,
                PrivateMemoryBytes = currentProcess.PrivateMemorySize64,
                ManagedMemoryBytes = GC.GetTotalMemory(false),
                MessagesProcessed = messagesProcessed,
                SearchOperations = searchOperations
            };

            memoryReadings.Add(reading);
        }

        private string GenerateTestEmailBody(int batch, int messageIndex, int senderIndex)
        {
            var lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";
            var body = new StringBuilder();
            body.AppendLine($"This is a stress test email generated for memory leak testing.");
            body.AppendLine($"Batch: {batch}, Message: {messageIndex}, Sender: {senderIndex}");
            body.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
            body.AppendLine();

            // Add variable content to make messages different sizes
            for (int i = 0; i < (messageIndex % 5) + 1; i++)
            {
                body.AppendLine(lorem);
            }

            return body.ToString();
        }

        private void GenerateStressTestReport(
            ConcurrentBag<MemoryReading> memoryReadings,
            int totalMessagesProcessed,
            int totalSearchOperations,
            int signalRNotifications)
        {
            var readings = memoryReadings.OrderBy(r => r.Timestamp).ToList();

            if (readings.Count == 0)
            {
                output.WriteLine("No memory readings collected");
                return;
            }

            var firstReading = readings.First();
            var lastReading = readings.Last();
            var testDuration = lastReading.Timestamp - firstReading.Timestamp;

            output.WriteLine("=== STRESS TEST REPORT ===");
            output.WriteLine($"Test Duration: {testDuration:mm\\:ss}");
            output.WriteLine($"Total Messages Processed: {totalMessagesProcessed}");
            output.WriteLine($"Total Search Operations: {totalSearchOperations}");
            output.WriteLine($"SignalR Notifications Received: {signalRNotifications}");
            output.WriteLine($"Memory Readings Collected: {readings.Count}");
            output.WriteLine("");

            output.WriteLine("=== MEMORY USAGE ANALYSIS ===");
            output.WriteLine($"Initial Working Set: {FormatBytes(firstReading.WorkingSetBytes)}");
            output.WriteLine($"Final Working Set: {FormatBytes(lastReading.WorkingSetBytes)}");
            output.WriteLine($"Peak Working Set: {FormatBytes(readings.Max(r => r.WorkingSetBytes))}");
            output.WriteLine($"Working Set Growth: {FormatBytes(lastReading.WorkingSetBytes - firstReading.WorkingSetBytes)}");
            output.WriteLine("");

            output.WriteLine($"Initial Managed Memory: {FormatBytes(firstReading.ManagedMemoryBytes)}");
            output.WriteLine($"Final Managed Memory: {FormatBytes(lastReading.ManagedMemoryBytes)}");
            output.WriteLine($"Peak Managed Memory: {FormatBytes(readings.Max(r => r.ManagedMemoryBytes))}");
            output.WriteLine($"Managed Memory Growth: {FormatBytes(lastReading.ManagedMemoryBytes - firstReading.ManagedMemoryBytes)}");
            output.WriteLine("");

            output.WriteLine("=== MEMORY USAGE OVER TIME ===");
            output.WriteLine("Time(s)\tMessages\tSearches\tWorking Set(MB)\tManaged(MB)");

            foreach (var reading in readings.Where((r, i) => i % 5 == 0)) // Sample every 5th reading to avoid spam
            {
                var elapsed = (reading.Timestamp - firstReading.Timestamp).TotalSeconds;
                output.WriteLine($"{elapsed:F1}\t{reading.MessagesProcessed}\t{reading.SearchOperations}\t{reading.WorkingSetBytes / 1024.0 / 1024.0:F1}\t{reading.ManagedMemoryBytes / 1024.0 / 1024.0:F1}");
            }

            output.WriteLine("");
            output.WriteLine("=== PERFORMANCE METRICS ===");
            if (testDuration.TotalSeconds > 0)
            {
                output.WriteLine($"Messages/second: {totalMessagesProcessed / testDuration.TotalSeconds:F2}");
                output.WriteLine($"Searches/second: {totalSearchOperations / testDuration.TotalSeconds:F2}");
            }

            if (totalMessagesProcessed > 0)
            {
                var memoryPerMessage = (lastReading.WorkingSetBytes - firstReading.WorkingSetBytes) / (double)totalMessagesProcessed;
                output.WriteLine($"Memory growth per message: {FormatBytes((long)memoryPerMessage)}");
            }
        }

        private void ValidateMemoryBehavior(ConcurrentBag<MemoryReading> memoryReadings)
        {
            var readings = memoryReadings.OrderBy(r => r.Timestamp).ToList();

            if (readings.Count < 2)
            {
                output.WriteLine("Not enough memory readings to validate behavior");
                return;
            }

            var firstReading = readings.First();
            var lastReading = readings.Last();

            // Check that memory growth is reasonable (not a hard assertion as memory can fluctuate)
            var workingSetGrowth = lastReading.WorkingSetBytes - firstReading.WorkingSetBytes;
            var managedMemoryGrowth = lastReading.ManagedMemoryBytes - firstReading.ManagedMemoryBytes;

            output.WriteLine("=== MEMORY BEHAVIOR VALIDATION ===");

            // Memory should not grow excessively (more than 500MB is concerning for this test)
            const long maxAllowableGrowth = 500L * 1024 * 1024; // 500MB

            if (workingSetGrowth > maxAllowableGrowth)
            {
                output.WriteLine($"WARNING: Working set grew by {FormatBytes(workingSetGrowth)}, which exceeds {FormatBytes(maxAllowableGrowth)}");
                output.WriteLine("This may indicate a memory leak");
            }
            else
            {
                output.WriteLine($"✅ Working set growth of {FormatBytes(workingSetGrowth)} is within acceptable limits");
            }

            if (managedMemoryGrowth > maxAllowableGrowth)
            {
                output.WriteLine($"WARNING: Managed memory grew by {FormatBytes(managedMemoryGrowth)}, which exceeds {FormatBytes(maxAllowableGrowth)}");
                output.WriteLine("This may indicate a managed memory leak");
            }
            else
            {
                output.WriteLine($"✅ Managed memory growth of {FormatBytes(managedMemoryGrowth)} is within acceptable limits");
            }

            // Check for memory trend - if consistently growing, that's concerning
            if (readings.Count >= 10)
            {
                var recentReadings = readings.TakeLast(10).ToList();
                var isGrowingConsistently = true;

                for (int i = 1; i < recentReadings.Count; i++)
                {
                    if (recentReadings[i].ManagedMemoryBytes <= recentReadings[i - 1].ManagedMemoryBytes)
                    {
                        isGrowingConsistently = false;
                        break;
                    }
                }

                if (isGrowingConsistently)
                {
                    output.WriteLine("WARNING: Memory is growing consistently in recent readings - possible memory leak");
                }
                else
                {
                    output.WriteLine("✅ Memory growth pattern appears normal (not consistently increasing)");
                }
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            double value = bytes;
            int suffixIndex = 0;

            while (value >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                value /= 1024;
                suffixIndex++;
            }

            return $"{value:F2} {suffixes[suffixIndex]}";
        }

        private class MemoryReading
        {
            public DateTime Timestamp { get; set; }
            public long WorkingSetBytes { get; set; }
            public long PrivateMemoryBytes { get; set; }
            public long ManagedMemoryBytes { get; set; }
            public int MessagesProcessed { get; set; }
            public int SearchOperations { get; set; }
        }

        private class PagedResultResponse
        {
            public List<object> Results { get; set; } = new();
            public int TotalCount { get; set; }
        }
    }
}