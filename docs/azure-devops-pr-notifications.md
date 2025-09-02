# Azure DevOps PR Build Failure Notifications

This document explains how the Azure DevOps pipeline automatically notifies @copilot when a build triggered by a GitHub Pull Request fails.

## How It Works

The Azure DevOps pipeline (`azure-pipelines.yml`) includes a special stage called `NotifyOnFailure` that:

1. **Triggers only on PR build failures**: Uses condition `and(failed(), eq(variables['Build.Reason'], 'PullRequest'))`
2. **Posts a GitHub comment**: Calls the GitHub API to comment on the PR
3. **Mentions @copilot**: The comment includes `@copilot` to notify the AI assistant
4. **Provides build details**: Includes build ID, URL, and commit information

## Setup Requirements

### 1. GitHub Personal Access Token

Create a GitHub Personal Access Token with the following permissions:
- `repo` scope (for private repositories)
- `public_repo` scope (for public repositories)

### 2. Azure DevOps Secret Variable

In your Azure DevOps project:

1. Go to **Pipelines** → **Library** → **Variable groups**
2. Create a new variable group or edit existing one
3. Add a new variable:
   - **Name**: `GITHUB_TOKEN`
   - **Value**: Your GitHub Personal Access Token
   - **Keep this value secret**: ✅ (checked)

### 3. Link Variable Group to Pipeline

In the pipeline settings, link the variable group containing `GITHUB_TOKEN` to make it available to the pipeline.

## Pipeline Configuration

The notification stage is configured as follows:

```yaml
- stage: NotifyOnFailure
  displayName: Notify on Build Failure
  condition: and(failed(), eq(variables['Build.Reason'], 'PullRequest'))
  dependsOn: 
    - Build
  jobs:
    - job: NotifyCopilot
      displayName: Notify Copilot of Build Failure
      # ... PowerShell script to post GitHub comment
```

## Comment Format

When a build fails, the following comment will be posted to the PR:

```
@copilot The build triggered by this PR has failed. Please investigate and fix the issues.

**Build Details:**
- Build ID: [Build ID]
- Build URL: [Link to Azure DevOps build]
- Commit: [Commit SHA]

Please check the build logs and address any compilation errors, test failures, or other issues.
```

## Testing

To test the notification system:

1. Create a PR that will cause the build to fail (e.g., introduce a compilation error)
2. Push the changes to trigger the Azure DevOps build
3. Verify that the build fails and a comment is posted to the PR mentioning @copilot

## Troubleshooting

### Common Issues

1. **No comment posted**: 
   - Verify `GITHUB_TOKEN` is configured correctly
   - Check that the token has appropriate permissions
   - Ensure the variable group is linked to the pipeline

2. **Authentication errors**:
   - Verify the GitHub token is valid and not expired
   - Check that the token has `repo` or `public_repo` scope

3. **Stage not triggered**:
   - Confirm the build actually failed (not cancelled or skipped)
   - Verify the build was triggered by a PR (`Build.Reason` = 'PullRequest')

### Debugging

Enable verbose logging by adding this to the PowerShell script:

```powershell
Write-Host "Build Reason: $(Build.Reason)"
Write-Host "PR Number: $(System.PullRequest.PullRequestNumber)"
Write-Host "Build Status: $(Agent.JobStatus)"
```

## Security Considerations

- Store the GitHub token as a secret variable in Azure DevOps
- Use a token with minimal required permissions
- Consider using GitHub App authentication for enhanced security
- Regularly rotate the GitHub token