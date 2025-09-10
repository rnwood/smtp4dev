# Azure DevOps PR Build Failure Notifications

This document explains how the Azure DevOps pipeline automatically notifies @copilot when a build triggered by a GitHub Pull Request fails, including detailed error information.

## How It Works

The Azure DevOps pipeline (`azure-pipelines.yml`) includes a special stage called `NotifyOnFailure` that:

1. **Triggers only on PR build failures**: Uses condition `and(failed(), eq(variables['Build.Reason'], 'PullRequest'))`
2. **Checks PR assignees**: Verifies that @copilot is assigned to the PR before posting notifications (supports multiple assignees)
3. **Posts a concise GitHub comment**: Calls the GitHub API to comment on the PR with build URL and instructions
4. **Directs @copilot to Azure DevOps**: Instructs @copilot to navigate to the build URL to fetch detailed error information directly
5. **Includes commit verification**: Provides the commit hash and instructs @copilot to verify it's working on the most recent commit
6. **Provides build details**: Includes build ID, URL, and commit information for navigation

## Assignment Requirement

**Important**: The notification system only posts comments to PRs that are assigned to @copilot. This prevents unnecessary notifications on PRs that are not being worked on by the automated assistant.

The system checks the PR assignees before posting any comments:
- If @copilot is among the assignees (there may be multiple assignees), the notification will be posted
- If @copilot is not assigned to the PR, no notification will be sent
- If the assignee check fails due to API issues, the notification will proceed as a fallback

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

### 3. Azure DevOps Build Service Permissions

The pipeline uses the built-in `System.AccessToken` to access build logs via the Azure DevOps REST API. Ensure the build service has the necessary permissions:

1. Go to **Project Settings** → **Repositories** → **Security**
2. Find "Build Service" accounts
3. Ensure they have "Read" permissions for the repository

### 4. Link Variable Group to Pipeline

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
@copilot The build triggered by this PR has failed. **Please access the build logs directly from Azure DevOps to investigate and fix the issues.**

🚨 IMPORTANT: Only proceed if this is the most recent commit on your branch. If a newer commit exists, ignore this notification.

**Build Details:**
- Build ID: [Build ID]
- Build URL: [Link to Azure DevOps build] (Click to access full build logs and details)
- Commit: [Commit SHA]
- Build Status: FAILED

**Instructions for @copilot:**
1. **First verify this is the latest commit** - If [Commit SHA] is not the most recent commit in this PR branch, ignore this notification
2. **Navigate to the build URL above** to access the complete Azure DevOps build logs and failure details
3. **Use your browser tools** to explore the failed build tasks, error messages, and full context
4. **Focus on the specific compilation errors, test failures, or other issues** shown in the Azure DevOps interface
5. **Make targeted fixes** based on the error messages you find in the build logs
6. Only build locally after making fixes to verify the solution

**Do not try to reproduce the build locally first - use the live build data from Azure DevOps for the most accurate failure information.**
```

## Build Navigation Instructions

The notification system provides @copilot with direct links to access build information:

1. **Provides build URL**: Links directly to the Azure DevOps build results page where @copilot can navigate using browser tools
2. **Includes commit verification**: Provides the exact commit SHA so @copilot can verify it's working on the most recent commit
3. **Directs to live data**: Instead of including static error logs in comments, @copilot accesses real-time build status and logs
4. **Enables full context**: @copilot can explore the complete Azure DevOps interface, including:
   - Individual task logs and error details
   - Build artifacts and test results
   - Timeline and execution details
   - Related build history and comparisons
5. **Prevents stale information**: By accessing live build data, @copilot always works with current information rather than potentially outdated logs

## Testing

To test the notification system:

1. Create a PR that will cause the build to fail (e.g., introduce a compilation error)
2. Push the changes to trigger the Azure DevOps build
3. Verify that the build fails and a comment is posted to the PR mentioning @copilot
4. Check that the comment includes the build URL and instructions for @copilot to navigate to Azure DevOps

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
   - **Check that @copilot is assigned to the PR** - notifications only go to PRs assigned to copilot

4. **@copilot ignores notification**:
   - Verify the commit SHA in the notification matches the latest commit in the PR branch
   - Check that the Azure DevOps build URL is accessible publicly
   - Ensure @copilot has browser capabilities enabled to navigate to Azure DevOps

### Debugging

Enable verbose logging by adding this to the PowerShell script:

```powershell
Write-Host "Build Reason: $(Build.Reason)"
Write-Host "PR Number: $(System.PullRequest.PullRequestNumber)"
Write-Host "Build Status: $(Agent.JobStatus)"
Write-Host "Build ID: $(Build.BuildId)"
Write-Host "Build URL: $buildUrl"
Write-Host "Commit SHA: $(Build.SourceVersion)"
```

## Security Considerations

- Store the GitHub token as a secret variable in Azure DevOps
- Use a token with minimal required permissions
- Consider using GitHub App authentication for enhanced security
- Regularly rotate the GitHub token
- Azure DevOps build URLs are publicly accessible for public repositories, allowing @copilot to navigate build details without authentication
- Build failure notifications contain only build URLs and commit hashes, not sensitive build content