# Wizard

The `{wizard}` directive renders an interactive, step-by-step guide. It asks one question at a time, shows progress, and ends on a recommendation with next steps and a summary of the reader's answers. Use it wherever readers have to choose between several options, such as which product or deployment fits them, or which type of page to write.

The questions, routing and result copy all come from a YAML file. docs-builder validates that file at build time, so a broken flow fails the build instead of breaking in the reader's browser.

## Usage

:::::{tab-set}

::::{tab-item} Output

:::{wizard} wizard-deployment-sample.yaml
:::

::::

::::{tab-item} Markdown

```markdown
:::{wizard} wizard-deployment-sample.yaml
:::
```

::::

:::::

The argument is the path to the YAML file. It's resolved relative to the page, or to the docs root if it starts with `/`. The path must stay inside the documentation source directory.

### Another example

The same directive can drive any decision. This one helps writers choose a content type. It has one variable, no scoring and a single button.

:::{wizard} wizard-content-type-sample.yaml
:::

## How the flow works

The reader moves through **screens**. Each screen has a `type`:

| Type | What it does |
|---|---|
| `question` | Shows a question with options. Picking an option moves to that option's `next` screen. |
| `route` | Shows nothing. It picks the next screen from the answers so far, so you can skip questions that don't apply. |
| `result` | The final recommendation. Exactly one screen must be a `result`. |

Each option can **set answers**, for example `set: { solution: security }`. At the end, the `results` section turns those answers into a recommendation (see [Results](#results)).

### Editing answers

On the result screen, **Your answers** lists every answer with a **Change** link. Changing an answer keeps the later answers that still apply. The reader returns to their result straight away, and only gets asked about questions that are new because their path changed.

## Config reference

### `meta`

```yaml
meta:
  title: Which Elastic is right for you?
  intro: Answer a few questions and we'll point you to the right fit.
  start_button: Let's go
  start: where           # id of the first screen
  progress_label: Your progress
```

### `screens`

```yaml
screens:
  goal:
    type: question
    progress: 60            # 0-100, drives the progress bar
    question: What do you want to do first?
    why: This picks the Elastic solution that fits your main goal.
    options:
      - label: Monitor logs, metrics and traces
        hint: Optional secondary text
        set: { solution: observability }
        next: after_goal
      - label: I'm just exploring
        next: after_goal
```

Each option can have these fields:

| Field | Description |
|---|---|
| `label` | Required. The text the reader clicks. Quote it if it contains a comma, for example `"Yes, on Elastic Cloud"`. |
| `hint` | Optional smaller text under the label. |
| `tags` | Optional chips under the label, for example the topics an option covers. |
| `wide` | In the `cards` layout, spans the full width. Good for a "not sure" option. |
| `set` | Writes answers, for example `{ solution: security }`. |
| `add` | Adds to a score, for example `{ serverless: 2 }`. A [scored variable](#results) picks the candidate with the highest score. |
| `flag` | Shows the matching note from `results.flag_notes` on the result screen. |
| `next` | Required on single-choice screens. The screen to show next. |
| `exclusive` | On multi-select screens, picking this option clears the others. |

#### Cards layout

Add `layout: cards` to a question to show its options as a grid of larger cards. Pair it with `tags` and `wide` for a rich first question:

```yaml
screens:
  goal:
    type: question
    layout: cards
    question: What are you trying to do?
    why: Pick the closest match. We'll ask a few follow-up questions.
    options:
      - label: Build AI features on my own data
        hint: Chatbots and assistants that understand meaning, not just keywords.
        tags: [Chatbots, Question answering, Recommendations]
        set: { solution: vectordb }
        next: ai_kind
      - label: Not sure yet? No problem.
        hint: We'll start you with a general-purpose setup so you can explore.
        wide: true
        set: { solution: elasticsearch }
        next: place
```

A screen with `multi: true` shows checkboxes and a **Continue** button. Put `next` on the screen instead of on each option.

Routes use conditions. Each condition has a `key` and exactly one of `equals`, `starts_with` or `in`:

```yaml
screens:
  after_goal:
    type: route
    rules:
      - when: { key: family, equals: self }
        next: done
    else: control
```

### Results

The `results` section turns answers into a recommendation. Everything the wizard recommends is a named **variable**. A heading like `{solution} on {deployment}` uses two variables, and a heading like `Write {type}` uses one.

```yaml
results:
  heading: "{solution} on {deployment} is right for you"

  variables:
    solution:
      answer: solution              # take the item named by the `solution` answer
      items:
        security:
          name: Security
          blurb: SIEM, endpoint protection and cloud posture on one platform.
          docs: { title: Get started with Security, href: https://www.elastic.co/docs/solutions/security/get-started }
    deployment:
      answer: deployment            # used when this answer was set directly...
      score: [serverless, hosted]   # ...otherwise the highest score wins, and ties go to the first one listed
      close_call_within: 1          # optional: how close the top two scores must be to count as a close call
      items:
        serverless: { name: Elastic Cloud Serverless, docs: { title: ..., href: ... } }
        hosted: { name: Elastic Cloud Hosted, docs: { title: ..., href: ... } }

  cta:                              # the first rule that matches wins
    - when: { key: deployment, equals: hosted }
      label: Create a deployment
      href: https://cloud.elastic.co/deployments/create
    - label: Create your {solution} project   # no `when`: the fallback
      href_from: solution                     # use that item's docs link

  extra_docs:                       # optional extra card, the first matching rule wins
    - when: { key: family, in: [cloud, self] }
      title: Moving from another tool?
      href: https://www.elastic.co/docs/manage-data/migrate

  flag_notes:
    check_requirements: Check that it's available for your match before you start.
  close_call_note: "Good to know: {other} is also a strong fit."
```

A variable takes its value from an `answer`, from a `score` list, or from both. An answer wins when it's set.

Some details only apply on some paths, such as a Serverless tier that doesn't exist for a self-managed deployment. Two fields handle that:

| Field | Description |
|---|---|
| `optional` | Skip the variable when it has nothing to show, instead of failing the build. |
| `when` | Only resolve the variable when a condition holds. It can test answers or variables declared before it, so declare the variable it depends on first. |

```yaml
variables:
  deployment: { ... }
  tier:
    answer: tier
    optional: true
    when: { key: deployment, equals: serverless }
    items: { ... }
```

In text, `{name}` inserts the chosen item's name for the variable `name`. In `cta` and `extra_docs`, a condition's `key` can be an answer or a variable, and a variable's value is the key of its chosen item. Use `href_from: name` to link to the chosen item's docs page.

## Build-time validation

docs-builder reports an error, and renders nothing, when it finds any of these:

* An unknown key, which usually means a typo or an unquoted comma in a flow-style label.
* A `next` or route target that doesn't exist, or a screen that can never be reached.
* A route or condition that tests an answer no option ever sets.
* A path that reaches the result with an answer that has no matching item, or without an answer a variable needs.
* A `{placeholder}` that isn't a variable, a `cta` with no fallback rule, or a flag with no note.
* A link that doesn't start with `https://`, `http://` or `/`.

docs-builder also **warns** about a question that can't change anything the result uses, because none of its options set an answer, score or flag that's read, and they all lead to the same screen. Asking it only makes the wizard longer, so remove it or make an option do something.

## Writing a good question journey

* **Every question should change the outcome.** If it doesn't, cut it. The warning above catches the obvious cases.
* **Start with what the reader is trying to do, not with your product names.** Ask in the reader's own words ("Add search to my app", "Find and stop security threats") and use `tags` for example scenarios they'll recognize. Product names belong in the result. Then ask where they'll run it.
* **Use facts, not guesses, to separate options.** A good question matches a documented difference between the options, such as a limit, a supported region or a feature that only one option has.
* **End on something to do.** Give each path a specific first step (for example a `first_step` variable) so the result isn't only a product name.
* **Always allow "I'm not sure".** Map it to the safest default and say so in the `hint`.

## Theming

The component keeps its styles in a shadow root. Page authors and site themes can restyle it with CSS custom properties on the `docs-wizard` element:

| Property | Default | Use |
|---|---|---|
| `--wizard-accent` | `#0b64dd` | Buttons, links, selected options and the progress bar. |
| `--wizard-accent-tint` | `#eaf2fd` | Hover and selected backgrounds. |
| `--wizard-ink` | `#1a1c21` | Text. |
| `--wizard-muted` | `#5a6275` | Secondary text. |
| `--wizard-line` | `#d9dee8` | Borders. |
| `--wizard-surface` | `#fff` | Card and option background. |
| `--wizard-radius` | `14px` | Card corner radius. |

## Accessibility

The component uses real buttons, moves focus to the new question heading on each step, and announces changes through a live region. The answers summary on the result screen is a native collapsed disclosure.
