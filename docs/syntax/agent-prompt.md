# Agent prompt

The `{agent-prompt}` directive adds a copyable prompt that readers paste into an AI coding agent. The agent then does the page's steps for them and reports what it did.

The prompt isn't shown on the page. Readers see a compact card with a title, a one-line description, a **Review prompt** button that shows the number of characters, and a **Copy prompt** button. **Review prompt** opens a dialog with the full text and a copy button.

## Usage

Put the prompt in a fenced code block inside the directive. The text is copied exactly as written, so numbered lists and blank lines are preserved. The fenced block is required.

:::::{tab-set}

::::{tab-item} Output

:::{agent-prompt}
:title: Run this quickstart with your AI agent

```text
I want to learn how semantic search works in Elasticsearch while you set it up for me. Read the Markdown version of the quickstart at
https://www.elastic.co/docs/solutions/search/get-started/semantic-search.md
and do the steps for me.

1. Ask me where Elasticsearch will run and for my connection details. Never print the API key back or write it to a file I might commit.
2. Work through the quickstart's steps in order, doing each one for me and using the page's own examples.
3. Check your work: read back what you created and compare it with what the page says should happen.
4. Finish with a short report: what I set out to learn, a table of the steps you ran with what each did and why, what I learned, and three next steps.
5. List what you created and the command that removes it. Do not delete anything unless I ask.

Ask me before you create anything that costs money or delete anything.
```
:::

::::

::::{tab-item} Markdown

````markdown
:::{agent-prompt}
:title: Run this quickstart with your AI agent

```text
I want to learn how semantic search works in Elasticsearch while you set it up for me. Read the Markdown version of the quickstart at
https://www.elastic.co/docs/solutions/search/get-started/semantic-search.md
and do the steps for me.

1. Ask me where Elasticsearch will run and for my connection details. Never print the API key back or write it to a file I might commit.
2. Work through the quickstart's steps in order, doing each one for me and using the page's own examples.
3. Check your work: read back what you created and compare it with what the page says should happen.
4. Finish with a short report: what I set out to learn, a table of the steps you ran with what each did and why, what I learned, and three next steps.
5. List what you created and the command that removes it. Do not delete anything unless I ask.

Ask me before you create anything that costs money or delete anything.
```
:::
````

::::

:::::

## Properties

| Property | Required | Description |
|---|---|---|
| `:title:` | No | Heading of the card and the dialog. Defaults to "Get started with your AI agent". |
| `:description:` | No | One line under the heading. Defaults to "Copy this prompt into an AI coding agent that can run commands. It does the steps on this page for you and reports what it did." |

The directive emits a build error if the body has no fenced code block. The prompt is left out of the LLM and plain text renderings of the page.

## Write a prompt that works

The agent runs the prompt with the reader's permissions, so treat it like code.

- **Say who it is for.** The prompt needs an agent that can run commands, such as a coding agent in a terminal or an editor. A chat window can't run it. Keep that sentence in the description if you override it.
- **Point at the page.** Link to the page and tell the agent to follow it. Don't copy the page's explanations into the prompt, because they drift.
- **Ask for a final report.** Agents tend to work silently and write one closing message. Ask for the learning goals, a table of the steps with what each did and why, what the reader learned, and next steps in that message. A request to explain each command as it runs is mostly ignored.
- **Ask the agent to check its work.** For example, read back document counts or mappings and compare them with what the page says should happen.
- **Keep deletion out.** Don't let the prompt run the page's cleanup step. Ask the agent to list what it created and the command that removes it.
- **Keep the guardrails.** Ask before anything that costs money, and never print or save an API key.
- **Keep it short.** A prompt of about 20 lines is enough. The card previews only the start, so make the first lines stand on their own.
- **Check the page's code blocks.** The Markdown version of a page omits the method and path line (for example `PUT /books`) of Console code blocks. If the prompt points at the Markdown version of a page with Console steps, tell the agent to take the method and path from the surrounding text.
- **Test it.** Run the prompt in at least two agents against a throwaway environment before you publish it.

## Telemetry

When the site's telemetry is enabled, the directive sends these events. The prompt text is never sent, only its length and the block's title.

| Event | Sent when |
|---|---|
| `agent_prompt_viewed` | The card is at least half visible, once per page view. This is the denominator for the other rates. |
| `agent_prompt_review_opened` | A reader opens the dialog with **Review prompt**. |
| `agent_prompt_copied` | The prompt was copied to the clipboard. Sent only when the copy succeeds. |
| `agent_prompt_copy_failed` | The clipboard write failed. Carries `error.type`. |

Every event carries these attributes, in addition to `event.name`, `url.path` and `url.full`:

| Attribute | Description |
|---|---|
| `agent_prompt.location` | The control involved: `inline` (the card on the page) or `dialog`. |
| `agent_prompt.title` | The block's title. |
| `agent_prompt.length` | The prompt length in characters. |
| `agent_prompt.index` | Zero-based position of the block among the prompts on the page. |
