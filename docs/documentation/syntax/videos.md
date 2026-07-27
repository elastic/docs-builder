# Videos

Videos can supplement documentation to demonstrate features, workflows, or complex procedures. Embed videos using standard Markdown link and image syntax.

:::{important}
For integrations documentation, any **file paths or custom URL slugs** in video links must use lowercase characters only. Mixed-case paths cause build failures. Note that Vidyard video IDs (like `eT92arEbpRddmSM4JeyzdX`) are assigned by the service and are acceptable as-is.
:::

## Elastic-hosted videos (recommended)

For Elastic-hosted videos on `videos.elastic.co`, use a clickable thumbnail that links to the video. This displays a preview image that readers can click to watch the video.

```markdown
[![Video description](https://play.vidyard.com/VIDEO_ID.jpg)](https://videos.elastic.co/watch/VIDEO_ID?)
```

**Example:**

```markdown
[![Attack Discovery video](https://play.vidyard.com/eT92arEbpRddmSM4JeyzdX.jpg)](https://videos.elastic.co/watch/eT92arEbpRddmSM4JeyzdX?)
```

This renders as a clickable thumbnail image. When clicked, it opens the video on `videos.elastic.co`.

### Finding the video ID

The video ID is the alphanumeric string in the Vidyard URL. For example, in `https://videos.elastic.co/watch/eT92arEbpRddmSM4JeyzdX`, the video ID is `eT92arEbpRddmSM4JeyzdX`.

Use this same ID for both:
- The thumbnail URL: `https://play.vidyard.com/VIDEO_ID.jpg`
- The video URL: `https://videos.elastic.co/watch/VIDEO_ID?`

## YouTube videos

For YouTube videos, use a simple text link:

```markdown
[Video title](https://www.youtube.com/watch?v=VIDEO_ID)
```

**Example:**

```markdown
Refer to the [Threadpool Rejections video](https://www.youtube.com/watch?v=auZJRXoAVpI) for a troubleshooting walkthrough.
```

## Best practices

**DO:**

- ✅ Introduce videos with context, such as "The following video demonstrates these steps (click to watch)."
- ✅ Use descriptive alt text that explains the video content
- ✅ Use lowercase file paths and custom URL slugs in integrations documentation
- ✅ Use Elastic-hosted videos on `videos.elastic.co` when available

**DON'T:**

- ❌ Use mixed-case file paths or custom URL slugs in integrations documentation—this causes build failures
- ❌ Rely solely on videos for critical information — always provide text alternatives
- ❌ Embed videos directly in the page—use clickable thumbnails instead
