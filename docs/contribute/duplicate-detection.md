# Duplicate Issue Detection

The docs-builder repository includes an automated workflow that helps identify potential duplicate issues using a two-step AI-powered analysis approach.

## How It Works

1. **Trigger**: The workflow is triggered when a new issue is created in the repository.
2. **First AI Call**: Sends all existing issue titles and numbers to GitHub Models to get the top 5 most similar issues in JSON format.
3. **Second AI Call**: Performs detailed analysis on the top 5 candidates using their full content (title + body).
4. **Comment**: If potential duplicates are found, the workflow posts a comment on the new issue with links to similar issues.

## Workflow Features

- **Pure AI Analysis**: Relies entirely on GitHub Models for duplicate detection without pre-filtering algorithms.
- **Two-Step Process**: First identifies candidates by title similarity, then performs detailed analysis with full content.
- **JSON-Structured Responses**: Uses structured JSON responses for reliable parsing of AI analysis results.
- **Comprehensive Coverage**: Analyzes all existing open issues (up to 100) in the first pass.
- **Fallback Mechanism**: If JSON parsing fails, falls back to text pattern matching.
- **Categorized Results**: Distinguishes between "likely duplicates" and "similar issues" to help maintainers prioritize.
- **Non-Intrusive**: Only comments when potential duplicates are found, doesn't interfere with normal issue workflow.

## AI Analysis Process

### Step 1: Title-Based Candidate Selection
- Sends new issue title and description along with all existing issue titles
- AI responds with JSON containing top 5 most similar issues
- Each candidate includes issue number and similarity level (high/medium)

### Step 2: Detailed Content Analysis  
- Performs deep analysis on the top 5 candidates using full issue bodies
- AI provides detailed comparison with reasoning
- Results in final classification: DUPLICATE, SIMILAR, or DIFFERENT

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

- **Permissions**: Read access to repository content, write access to issues, and read access to GitHub Models
- **Two AI Calls**: Structured for candidate selection and detailed analysis
- **Error Handling**: Graceful handling of API failures with fallback mechanisms

## Benefits

- **Reduces Maintenance Overhead**: Helps maintainers quickly identify duplicate issues
- **Improves Issue Quality**: Encourages users to search existing issues before creating new ones
- **Enhances Collaboration**: Directs users to existing discussions where they can contribute
- **High Accuracy**: Two-step analysis ensures thorough evaluation of potential duplicates

## Technical Details

- **GitHub Models Integration**: Uses the GitHub Models API with GPT-4o-mini for semantic analysis
- **Two-Step Analysis**: First pass identifies candidates, second pass performs detailed analysis
- **JSON Responses**: Structured responses for reliable parsing and error handling
- **Comprehensive Scope**: Analyzes all open issues without pre-filtering
- **API Efficiency**: Typically requires only 2 AI API calls regardless of repository size

The workflow is designed to provide accurate duplicate detection through comprehensive AI analysis while maintaining simplicity and reliability.