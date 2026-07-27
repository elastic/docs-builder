# Link validation

:::{warning}
Old development note — likely to be deleted or substantially rewritten.
:::

* See the [RFC](https://docs.google.com/document/d/1fZNeJCVLKu19s4WIKkkqrHyE9YlWQHNed94Y_V7ofRI/edit?tab=t.0#heading=h.z8tixe192fr4).
* Infrastructure lives in [docs-infra](https://github.com/elastic/docs-infra).

```mermaid
flowchart TD
    subgraph buildProcess [Repository Build Process]
        direction LR
        subgraph repos [Repositories]
            A[Repository A] --> Z1[Link validation]
            B[Repository B] --> Z2[Link validation]
            C[Repository C] --> Z3[Link validation]
        end
        Z1 & Z2 & Z3 -->|"validation succeeds"| E[Generate links.json]
        E --> H[Upload links.json to S3]
    end

    subgraph linkIndex [AWS Link Index]
        I[Amazon S3 Bucket] --> J[CloudFront Distribution]
    end

    subgraph assembler [Assembler]
        X["Validate links and build docs"]
    end

    subgraph validation [Link validation process]
        subgraph changes [Changes to md files]
            Q[Add external links] --> K[Docs build kicks off]
            R[Remove Markdown files] --> K
        end
        K --> L[Download links.json from CloudFront]
        L --> M{Link Validation}
        M -->|"All links valid"| N[Build succeeds]:::success
        M -->|"Broken links found"| O[Build fails]:::warning
    end

    H --> I
    J --> X
    J --> L
```
