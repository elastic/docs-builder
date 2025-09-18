# Duplicate Issue Detection

The docs-builder repository includes an automated workflow that helps identify potential duplicate issues using AI-powered analysis with optimized efficiency.

## How It Works

1. **Trigger**: The workflow is triggered when a new issue is created in the repository.
2. **Pre-filtering**: Uses lightweight text similarity to identify candidate issues (reduces AI API calls by ~80-90%).
3. **AI Analysis**: Uses GitHub Models (GPT-4o-mini) to analyze promising candidates in batches for efficiency.
4. **Comment**: If potential duplicates are found, the workflow posts a comment on the new issue with links to similar issues.

## Workflow Features

- **Efficient Processing**: Pre-filters issues using text similarity before AI analysis, reducing API calls from potentially 100+ to typically 1-2.
- **Batch AI Analysis**: Processes multiple issue comparisons in a single API call for maximum efficiency.
- **Smart Candidate Selection**: Focuses AI analysis on the most promising candidates based on title and content similarity.
- **Fallback Mechanism**: If the AI service is unavailable, it uses the pre-filtering results.
- **Categorized Results**: Distinguishes between "likely duplicates" and "similar issues" to help maintainers prioritize.
- **Non-Intrusive**: Only comments when potential duplicates are found, doesn't interfere with normal issue workflow.

## Performance Optimizations

- **Pre-filtering**: Reduces candidates from 100+ issues to typically 5-20 relevant ones
- **Batch Processing**: Single AI API call instead of individual calls per issue
- **Early Termination**: Stops processing when sufficient duplicates are found
- **Smart Limits**: Analyzes only top 20 most relevant candidates, processes max 10 in AI batch

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
- **Efficient Processing**: Pre-filtering and batch processing to minimize AI API calls
- **Error Handling**: Graceful handling of API failures with fallback mechanisms

## Benefits

- **Reduces Maintenance Overhead**: Helps maintainers quickly identify duplicate issues
- **Improves Issue Quality**: Encourages users to search existing issues before creating new ones
- **Enhances Collaboration**: Directs users to existing discussions where they can contribute
- **Cost Effective**: Optimized to minimize AI API usage while maintaining accuracy

## Technical Details

- **GitHub Models Integration**: Uses the GitHub Models API with GPT-4o-mini for semantic analysis
- **Pre-filtering Algorithm**: Text similarity analysis to identify relevant candidates
- **Batch Processing**: Compares up to 10 issues in a single AI API call
- **Performance**: Reduces API calls by 80-90% compared to individual comparisons

The workflow is designed to be both helpful and efficient, providing accurate duplicate detection while minimizing resource usage.