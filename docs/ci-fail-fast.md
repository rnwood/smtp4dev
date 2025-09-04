# CI Fail-Fast Configuration

This document explains the fail-fast configuration implemented in the Azure DevOps pipeline to speed up PR iteration by providing quick feedback when builds fail.

## Overview

The pipeline is configured to fail fast when critical build tasks fail, so developers don't have to wait for the complete build matrix to finish before getting feedback on failures. This significantly reduces the time to get feedback on PRs.

## How It Works

### 1. BuildMatrix Fail-Fast

The core `BuildMatrix` job builds and tests multiple platform targets in parallel:
- `noruntime` - Platform-independent build
- `win-x64` - Windows 64-bit build with tests
- `linux-x64` - Linux 64-bit build with tests  
- `linux-musl-x64` - Linux MUSL build
- `win-arm64` - Windows ARM64 build
- `linux-arm` - Linux ARM build

**Fail-Fast Behavior**: When `failFast: true` is enabled on the matrix strategy, if any platform build fails, Azure DevOps will immediately cancel the remaining platform builds in the matrix. This provides quick feedback instead of waiting for all platforms to complete.

### 2. Job Dependencies

Non-critical build jobs depend on the `BuildMatrix` job:

- **`BuildGlobalTool`** - Depends on `BuildMatrix` success
- **`BuildDesktop`** - Depends on `BuildMatrix` success  
- **Docker builds** - Depend on `BuildMatrix` but can proceed even if some matrix jobs failed

**Fail-Fast Behavior**: If the `BuildMatrix` fails completely, dependent jobs like `BuildGlobalTool` and `BuildDesktop` will be skipped immediately rather than running and potentially failing later.

### 3. Stage-Level Dependencies

The pipeline stages have these dependencies:
- **`ReportCoverage`** - Depends on `Build` stage
- **`Release`** - Depends on `Build` stage  
- **`NotifyOnFailure`** - Runs when `Build` stage fails

**Fail-Fast Behavior**: When the `Build` stage fails fast, the `NotifyOnFailure` stage triggers immediately to notify developers, while `ReportCoverage` and `Release` are skipped.

## Benefits

1. **Faster Feedback**: Developers get notified of build failures within minutes instead of waiting 10-20+ minutes for all builds to complete
2. **Reduced Resource Usage**: Failed builds don't consume unnecessary CI resources
3. **Quicker Iteration**: PRs can be fixed and re-pushed sooner, improving development velocity
4. **Early Problem Detection**: Critical issues are surfaced immediately rather than being buried in later build logs

## Configuration Details

### Matrix Strategy Fail-Fast

```yaml
strategy:
  failFast: true
  matrix:
    # ... platform definitions
```

### Job Dependencies

```yaml
- job: BuildGlobalTool
  dependsOn: BuildMatrix
  condition: succeeded('BuildMatrix')
  
- job: DockerBuildLinux  
  dependsOn: BuildMatrix
  condition: succeededOrFailed('BuildMatrix')
```

## When Fail-Fast Triggers

Fail-fast behavior is triggered when:

1. **Compilation errors** in any platform build
2. **Test failures** in core platforms (win-x64, linux-x64)
3. **Infrastructure issues** (missing dependencies, environment setup failures)
4. **Timeout conditions** in any matrix job

## Exceptions

Some scenarios where builds continue despite failures:

1. **Docker builds** use `succeededOrFailed` condition, so they proceed even if some matrix platforms fail
2. **Expected test failures** (like IPv6 tests in CI environments) don't trigger fail-fast
3. **Non-test platforms** (linux-musl-x64, win-arm64, linux-arm) don't run tests, so compilation is the main failure point

## Monitoring

Developers can monitor fail-fast behavior through:

1. **Azure DevOps build logs** - Shows which jobs were cancelled due to fail-fast
2. **GitHub PR notifications** - The `NotifyOnFailure` stage comments on PRs with detailed error information
3. **Build timeline** - Visual representation of which jobs ran and which were skipped

## Best Practices

When fail-fast is triggered:

1. **Check the first failure** - The initial failing job usually contains the root cause
2. **Review build logs carefully** - Error messages are included in PR notifications
3. **Fix core issues first** - Focus on compilation errors and test failures before platform-specific issues
4. **Test locally** - Use `dotnet build` and `dotnet test` to verify fixes before pushing

## Troubleshooting

If fail-fast is causing issues:

1. **Verify the error is legitimate** - Check if the failure is due to a real issue or infrastructure problem
2. **Check dependencies** - Ensure all required dependencies are restored
3. **Review job conditions** - Some jobs may be skipped due to dependency failures
4. **Contact maintainers** - If fail-fast is preventing necessary builds, the configuration can be adjusted

## Future Enhancements

Potential improvements to the fail-fast configuration:

1. **Platform-specific priorities** - Fail fast only on critical platforms (win-x64, linux-x64)
2. **Parallel test execution** - Further optimize test execution to reduce overall build time
3. **Selective Docker builds** - Only build Docker images for platforms that succeeded
4. **Conditional test execution** - Skip some tests in PR builds to focus on core functionality