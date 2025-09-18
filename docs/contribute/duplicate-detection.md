# Duplicate Issue Detection

The docs-builder repository includes an automated workflow that helps identify potential duplicate issues using AI-powered analysis.

## How It Works

1. **Trigger**: The workflow is triggered when a new issue is created in the repository.
2. **Analysis**: It uses GitHub Models (GPT-4o) to analyze the new issue content and compare it with existing open issues.
3. **Comment**: If potential duplicates are found, the workflow posts a comment on the new issue with links to similar issues.

## Workflow Features

- **AI-Powered Comparison**: Uses advanced language models to understand the semantic similarity between issues, not just keyword matching.
- **Fallback Mechanism**: If the AI service is unavailable, it falls back to basic text similarity analysis.
- **Categorized Results**: Distinguishes between "likely duplicates" and "similar issues" to help maintainers prioritize.
- **Non-Intrusive**: Only comments when potential duplicates are found, doesn't interfere with normal issue workflow.

## Example Output

When duplicates are detected, the workflow posts a comment like this:

```markdown
👋 **Potential duplicate issues detected**

This issue appears to be similar to existing open issues:

### 🚨 Likely Duplicates
- #123 - [Build fails with .NET 9](https://github.com/elastic/docs-builder/issues/123)

### 🔍 Similar Issues
- #456 - [Performance issues during build](https://github.com/elastic/docs-builder/issues/456)

Please review these issues to see if your issue is already covered. 
If this is indeed a duplicate, consider closing this issue and contributing to the existing discussion.

---
*This comment was automatically generated using AI to help identify potential duplicates.*
```

## Workflow Configuration

The workflow is defined in `.github/workflows/detect-duplicate-issues.yml` and includes:

- **Permissions**: Read access to repository content and write access to issues
- **Rate Limiting**: Built-in delays to respect API limits
- **Error Handling**: Graceful handling of API failures with fallback mechanisms

## Benefits

- **Reduces Maintenance Overhead**: Helps maintainers quickly identify duplicate issues
- **Improves Issue Quality**: Encourages users to search existing issues before creating new ones
- **Enhances Collaboration**: Directs users to existing discussions where they can contribute

## Technical Details

- **GitHub Models Integration**: Uses the GitHub Models API with GPT-4o for semantic analysis
- **Comparison Logic**: Analyzes both issue titles and descriptions for comprehensive matching
- **Performance**: Processes up to 100 existing issues with smart rate limiting

The workflow is designed to be helpful without being disruptive, only adding comments when genuine potential duplicates are identified.