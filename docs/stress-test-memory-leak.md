# Memory Leak Stress Test

## Purpose

This stress test was created to validate the memory leak fix in `MessagesController.GetSummaries()` that addresses issue #1479. The test simulates a high-load scenario to ensure that the search functionality does not consume excessive memory when handling large numbers of messages.

## What the Test Does

The stress test simulates a realistic production scenario by:

1. **Concurrent SMTP Senders**: Multiple threads sending emails via SMTP protocol
2. **Concurrent API Readers**: Multiple threads performing search operations via the REST API
3. **SignalR Monitoring**: Listening for real-time notifications when messages change
4. **Memory Monitoring**: Continuously tracking memory usage throughout the test
5. **Performance Reporting**: Generating detailed reports correlating memory usage with message count

## Test Scenarios

### Full Stress Test (`MessageSearchStressTest_ShouldNotLeakMemory`)
- **Duration**: 2 minutes
- **Messages**: 1,000 total (10 batches × 100 messages)
- **SMTP Senders**: 5 concurrent connections
- **API Readers**: 3 concurrent search operations
- **Search Frequency**: Every 500ms

### Quick CI Test (`MessageSearchStressTest_QuickCI_ShouldNotLeakMemory`)
- **Duration**: 30 seconds
- **Messages**: 150 total (3 batches × 50 messages)
- **SMTP Senders**: 2 concurrent connections
- **API Readers**: 2 concurrent search operations
- **Search Frequency**: Every 1000ms

## How It Validates the Fix

The test specifically validates that the memory leak fix works by:

1. **Database-Level Filtering**: Ensuring search operations use SQL WHERE clauses instead of loading all data into memory
2. **Memory Growth Tracking**: Monitoring that memory usage doesn't grow unboundedly
3. **Search Operation Validation**: Confirming that case-insensitive search works correctly under load
4. **Performance Metrics**: Measuring throughput and memory efficiency

## Memory Behavior Validation

The test validates memory behavior by:

- **Baseline Monitoring**: Recording initial memory usage
- **Continuous Tracking**: Sampling memory every 2 seconds during test execution
- **Growth Analysis**: Checking that memory growth stays within acceptable limits (< 500MB)
- **Trend Detection**: Identifying concerning patterns like consistently increasing memory usage

## Test Output

The test generates a comprehensive report including:

```
=== STRESS TEST REPORT ===
Test Duration: 02:00
Total Messages Processed: 1000
Total Search Operations: 240
SignalR Notifications Received: 50
Memory Readings Collected: 60

=== MEMORY USAGE ANALYSIS ===
Initial Working Set: 89.42 MB
Final Working Set: 92.15 MB
Peak Working Set: 95.73 MB
Working Set Growth: 2.73 MB

Initial Managed Memory: 45.23 MB
Final Managed Memory: 47.88 MB
Peak Managed Memory: 51.12 MB
Managed Memory Growth: 2.65 MB

=== MEMORY USAGE OVER TIME ===
Time(s) Messages Searches Working Set(MB) Managed(MB)
0.0     0        0        89.4             45.2
10.0    125      12       90.1             46.1
20.0    287      24       91.2             47.3
...

=== PERFORMANCE METRICS ===
Messages/second: 8.33
Searches/second: 2.00
Memory growth per message: 2.8 KB

=== MEMORY BEHAVIOR VALIDATION ===
✅ Working set growth of 2.73 MB is within acceptable limits
✅ Managed memory growth of 2.65 MB is within acceptable limits
✅ Memory growth pattern appears normal (not consistently increasing)
```

## Running the Test

### Prerequisites
- .NET 8.0 SDK
- Available ports for SMTP (2525) and HTTP (5000) services

### Command Line
```bash
# Run the full stress test
dotnet test --filter "FullyQualifiedName~MessageSearchStressTest_ShouldNotLeakMemory"

# Run the quick CI test
dotnet test --filter "FullyQualifiedName~MessageSearchStressTest_QuickCI_ShouldNotLeakMemory"
```

### Environment Variables (Optional)
You can customize the E2E test environment:
- `SMTP4DEV_E2E_WORKINGDIR`: Working directory for the test
- `SMTP4DEV_E2E_BINARY`: Path to smtp4dev binary
- `SMTP4DEV_E2E_USEDEFAULTDBPATH`: Use default database path (set to "1")

## Integration with CI/CD

The quick test (`MessageSearchStressTest_QuickCI_ShouldNotLeakMemory`) is designed for CI environments:
- **Short Duration**: 30 seconds to avoid CI timeouts
- **Reduced Load**: Fewer concurrent operations to work in constrained environments
- **Memory Validation**: Still validates the core memory leak fix

## Technical Details

### Key Components
- **SMTP Client**: Uses `System.Net.Mail.SmtpClient` for realistic email sending
- **HTTP Client**: Uses `HttpClient` for API requests
- **SignalR Client**: Uses `Microsoft.AspNetCore.SignalR.Client` for real-time notifications
- **Memory Monitoring**: Uses `Process.GetCurrentProcess()` and `GC.GetTotalMemory()`

### Search Operations
The test performs various search operations to exercise the fixed functionality:
- Subject searches
- From address searches  
- To address searches
- Combined field searches
- Empty searches (retrieve all messages)

### Concurrent Operations
- **Thread-Safe Counters**: Uses `Interlocked` operations for thread-safe counting
- **Concurrent Collections**: Uses `ConcurrentBag<T>` for memory readings
- **Cancellation Tokens**: Proper cancellation handling for clean shutdown

## Expected Results

**Before the Fix (Problematic)**:
- Memory would grow significantly (hundreds of MB)
- Possible `OutOfMemoryException` with large message counts
- Linear memory growth with message count

**After the Fix (Expected)**:
- Minimal memory growth (< 50MB typically)
- No memory exceptions
- Stable memory usage regardless of message count
- Efficient database-level filtering

## Troubleshooting

### Common Issues
1. **Port Conflicts**: Ensure ports 2525 and 5000+ are available
2. **Timeout Issues**: The test may timeout in slow environments - consider reducing load parameters
3. **Database Locks**: Ensure no other smtp4dev instances are running

### Test Failures
If the test fails with memory warnings:
1. Check if the memory leak fix is properly applied
2. Verify that search operations use `IQueryable` instead of `IEnumerable`
3. Ensure `.ToList()` is not called before applying search filters

This stress test provides confidence that the memory leak fix in PR #1479 effectively resolves the performance issues when searching through large numbers of messages.