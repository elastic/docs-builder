---
title: Link validation
---

* See the [RFC](https://docs.google.com/document/d/1fZNeJCVLKu19s4WIKkkqrHyE9YlWQHNed94Y_V7ofRI/edit?tab=t.0#heading=h.z8tixe192fr4).
* Infrastructure lives in [docs-infra](https://github.com/elastic/docs-infra).

The mermaid chart below is currently unsupported.

```
flowchart TD
    subgraph **Repository Build Process**
        direction LR
        subgraph Repositories
            A[Repository A] --> Z1
            B[Repository B] --> Z2
            C[Repository C] --> Z3
            Z1[Link validation process]
            Z2[Link validation process]
            Z3[Link validation process]
        end
        Z1 & Z2 & Z3 -->|If validation succeeds| E[Generate links.json]
        E -->|Extract external links and add to _external_links_ array| H
        H[Upload updated links.json to S3]
    end

    subgraph AWS **Link Index**
        H --> I[Amazon S3 Bucket]
        I --> J[CloudFront Distribution]
    end

    subgraph Assembler
        J --> X["Validate links and build docs (TBD)"]
    end

    subgraph **Link validation process**
        subgraph Changes to md files
            Q[Add External Links] --> K
            R[Remove Markdown Files] --> K
        end
        K[Docs build kicks off]
        K --> L[Download links.json files from CloudFront]
        L --> M{Link Validation}
        M -->|All Links Valid| N[Build Succeeds]
        M -->|Broken Links Found| O[Build Fails]
    end

    J --> L

    style N fill:#a3d9a5,color:#333
    style O fill:#f8a5a5,color:#333
```