Operate on the public changelog bundle registries owned by the scrubber Lambda.

Each group of published changelog artifacts — `bundle/{product}/` or `changelog/{org}/{repo}/{branch}/` — carries a `registry.json` manifest in the **public** bucket, produced exclusively by the scrubber Lambda from the bucket's actual state. These commands are the operator surface for that machinery: `reconcile` asks the Lambda to converge groups (via explicit queue messages — the CLI never writes to S3 itself), and `verify` reports, read-only, whether each public manifest matches its public listing.
