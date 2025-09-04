# Azure DevOps PR Build Failure Notifications

This document explains how the Azure DevOps pipeline automatically notifies @copilot when a build triggered by a GitHub Pull Request fails, including detailed error information.

## How It Works

The Azure DevOps pipeline (`azure-pipelines.yml`) includes a special stage called `NotifyOnFailure` that:

1. **Triggers only on PR build failures**: Uses condition `and(failed(), eq(variables['Build.Reason'], 'PullRequest'))`
2. **Checks PR assignees**: Verifies that @copilot is assigned to the PR before posting notifications (supports multiple assignees)
3. **Fetches detailed build error information**: Uses Azure DevOps REST API to retrieve build logs and error details
4. **Posts a comprehensive GitHub comment**: Calls the GitHub API to comment on the PR with error details
5. **Mentions @copilot with specific instructions**: The comment includes `@copilot` and instructs it to use the provided error details first
6. **Provides build details**: Includes build ID, URL, commit information, and actual error messages

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
@copilot The build triggered by this PR has failed. **Please use the error details provided below to investigate and fix the issues. Do not attempt to reproduce the build locally first - consult these error details directly.**

**Build Details:**
- Build ID: [Build ID]
- Build URL: [Link to Azure DevOps build]
- Commit: [Commit SHA]

**Build Error Details:**

**Failed Task: [Task Name]**
```
[Error messages and compilation errors]
```

**Instructions for @copilot:**
1. **Analyze the error details above first** - do not try to reproduce the build locally
2. Focus on the specific compilation errors, test failures, or other issues shown
3. Make targeted fixes based on the error messages provided
4. Only build locally after making fixes to verify the solution
```

## Error Detail Extraction

The notification system automatically:

1. **Fetches build timeline**: Uses Azure DevOps REST API to get build execution details
2. **Identifies failed tasks**: Finds tasks that resulted in failure status
3. **Retrieves task logs**: Downloads detailed logs for failed tasks
4. **Extracts error lines**: Filters log content for lines containing error indicators:
   - Keywords: `error`, `Error`, `ERROR`, `failed`, `Failed`, `FAILED`
   - Compiler errors: `CS####` (C# compiler errors)
   - MSBuild errors: `MSB####` (MSBuild errors)
5. **Limits output**: Shows up to 10 error lines per task and up to 3 failed tasks to keep comments manageable

## Testing

To test the notification system:

1. Create a PR that will cause the build to fail (e.g., introduce a compilation error)
2. Push the changes to trigger the Azure DevOps build
3. Verify that the build fails and a comment is posted to the PR mentioning @copilot
4. Check that the comment includes detailed error information from the build logs

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

4. **No error details in comment**:
   - Check that the build service has read permissions for build logs
   - Verify `System.AccessToken` is available in the pipeline context
   - Look for warnings in the pipeline logs about log retrieval failures

### Debugging

Enable verbose logging by adding this to the PowerShell script:

```powershell
Write-Host "Build Reason: $(Build.Reason)"
Write-Host "PR Number: $(System.PullRequest.PullRequestNumber)"
Write-Host "Build Status: $(Agent.JobStatus)"
Write-Host "Build ID: $(Build.BuildId)"
Write-Host "System Access Token Available: $($env:SYSTEM_ACCESSTOKEN -ne $null)"
```

## Security Considerations

- Store the GitHub token as a secret variable in Azure DevOps
- Use a token with minimal required permissions
- Consider using GitHub App authentication for enhanced security
- Regularly rotate the GitHub token
- The pipeline uses the built-in `System.AccessToken` which is automatically managed by Azure DevOps
- Error details are limited to reduce the risk of exposing sensitive information in comments