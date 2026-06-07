## Ensure data nodes have enough memory [_ensure_data_nodes_have_enough_memory]

{{es}} uses either the Hierarchical Navigable Small World ([HNSW](https://arxiv.org/abs/1603.09320)) algorithm or the Disk Better Binary Quantization ([DiskBBQ](https://www.elastic.co/search-labs/blog/diskbbq-elasticsearch-introduction)) algorithm for approximate kNN search.

HNSW is a graph-based algorithm which only works efficiently when most vector data is held in memory. You should ensure that data nodes have at least enough RAM to hold the vector data and index structures.

DiskBBQ is a clustering algorithm which can scale efficiently often on less memory than HNSW. Where HNSW typically performs poorly without sufficient memory to fit the entire structure in RAM, DiskBBQ scales linearly when using less available memory than the total index size. You can start with enough RAM to hold the vector data and index structures but, in most cases, you should be able to reduce your RAM allocation and still maintain good performance. In testing, as little as 1–5% of the index structure size (centroids and quantized vector data) loaded in off-heap RAM is necessary for reasonable performance for each set of queries that accesses largely overlapping clusters.

To check the size of the vector data, you can use the [Analyze index disk usage](https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-indices-disk-usage) API.

:::{tip}
For `float` vectors with `dim` greater than or equal to `384`, using a [`quantized`](elasticsearch://reference/elasticsearch/mapping-reference/dense-vector.md#dense-vector-quantization) index is highly recommended. Quantization can reduce off-heap RAM by 4×, 8×, or as much as 32×.
:::

:::{note}
{{es}} supports a maximum of 4,096 dimensions for `dense_vector` fields. Refer to the [`dense_vector` mapping reference](elasticsearch://reference/elasticsearch/mapping-reference/dense-vector.md#dense-vector-params) for supported parameters and limits.
:::

### Estimate disk usage [_estimate_disk_usage]

Disk usage for a `dense_vector` field consists of three components: raw vector storage, quantized vector storage (if quantization is enabled), and index structure overhead.

#### Raw vector storage

The raw (unquantized) vectors are always stored on disk regardless of quantization settings. The size depends on the `element_type`:

| `element_type` | Bytes per dimension | Disk per vector |
| --- | --- | --- |
| `float` | 4 | `num_dimensions × 4` |
| `bfloat16` | 2 | `num_dimensions × 2` |
| `byte` | 1 | `num_dimensions` |
| `bit` | 1/8 | `⌈num_dimensions / 8⌉` |

```{math}
\text{raw\_vector\_bytes} = \text{num\_vectors} \times \text{bytes\_per\_vector}
```

#### Quantized vector storage

When quantization is enabled, {{es}} stores both the raw vectors and an additional set of quantized vectors. This increases total disk usage but reduces off-heap RAM requirements. Quantized vector storage only applies to `float` and `bfloat16` element types.

| `quantization` | Additional bytes per vector |
| --- | --- |
| `int8` | `num_dimensions + 4` |
| `int4` | `⌈num_dimensions / 2⌉ + 4` |
| `bbq` | `⌈num_dimensions / 8⌉ + 14` |

```{math}
\text{quantized\_disk} = \text{num\_vectors} \times \text{quantized\_bytes\_per\_vector}
```

#### Index structure on disk

The index structure overhead depends on the algorithm used:

::::{tab-set}

:::{tab-item} HNSW
The HNSW graph stores neighbor connections for each vector. The default value for `m` (connections per node) is `16`.

```{math}
\text{hnsw\_graph\_bytes} = \text{num\_vectors} \times 4 \times m
```

With the default `m = 16`:

```{math}
\text{hnsw\_graph\_bytes} = \text{num\_vectors} \times 64
```
:::

:::{tab-item} Flat
The flat (brute-force) index has no additional index structure on disk. Only the raw and quantized vectors are stored.

```{math}
\text{flat\_index\_bytes} = 0
```
:::

:::{tab-item} DiskBBQ
DiskBBQ stores cluster centroids and quantized vectors within clusters. The default value for `vectors_per_cluster` is `384`.

First, compute the number of clusters:

```{math}
\text{num\_clusters} = \left\lceil \frac{\text{num\_vectors}}{\text{vectors\_per\_cluster}} \right\rceil
```

Then compute the centroid and quantized vector storage:

```{math}
\begin{align*}
\text{centroid\_bytes} &= \text{num\_clusters} \times \text{num\_dimensions} \times 4 \\
&+ \text{num\_clusters} \times (\text{num\_dimensions} + 14)
\end{align*}
```

```{math}
\text{quantized\_vector\_bytes} = \text{num\_vectors} \times \left(\left(\left\lceil \frac{\text{num\_dimensions}}{8} \right\rceil + 14 + 2\right) \times 2\right)
```

```{math}
\text{diskbbq\_total\_bytes} = \text{centroid\_bytes} + \text{quantized\_vector\_bytes}
```
:::

::::

#### Total disk per replica

```{math}
\text{total\_disk} = \text{raw\_vector\_bytes} + \text{quantized\_disk} + \text{index\_structure\_bytes}
```

### Estimate off-heap RAM [_estimate_off_heap_ram]

Off-heap RAM is used by the filesystem cache to hold vector data and index structures in memory for fast search. This is separate from the Java heap.

#### Vector data in RAM

The amount of vector data held in off-heap RAM depends on the `element_type` and `quantization`. When quantization is enabled, only the smaller quantized vectors need to be in RAM — the raw vectors are accessed from disk only during rescoring.

| `element_type` | `quantization` | RAM per vector |
| --- | --- | --- |
| `float` | none | `num_dimensions × 4` |
| `float` | `int8` | `num_dimensions + 4` |
| `float` | `int4` | `⌈num_dimensions / 2⌉ + 4` |
| `float` | `bbq` | `⌈num_dimensions / 8⌉ + 14` |
| `bfloat16` | none | `num_dimensions × 2` |
| `bfloat16` | `int8` | `num_dimensions + 4` |
| `bfloat16` | `int4` | `⌈num_dimensions / 2⌉ + 4` |
| `bfloat16` | `bbq` | `⌈num_dimensions / 8⌉ + 14` |
| `byte` | none | `num_dimensions` |
| `bit` | none | `⌈num_dimensions / 8⌉` |

```{math}
\text{vector\_ram} = \text{num\_vectors} \times \text{ram\_per\_vector}
```

#### Index structure in RAM

::::::{tab-set}

:::::{tab-item} HNSW
The HNSW graph must be fully loaded in memory for efficient search. The default value for `m` is `16`.

```{math}
\text{hnsw\_ram} = \text{num\_vectors} \times 4 \times m
```

**Total off-heap RAM for HNSW:**

```{math}
\text{total\_ram} = \text{vector\_ram} + \text{hnsw\_ram}
```
:::::

:::::{tab-item} Flat
The flat index has no graph structure. Only vector data needs to be in RAM.

```{math}
\text{total\_ram} = \text{vector\_ram}
```
:::::

:::::{tab-item} DiskBBQ
DiskBBQ is designed to work efficiently with only a fraction of the index in memory. In testing, as little as 1–5% of the total index structure (centroids + quantized vectors) loaded in off-heap RAM provides reasonable performance.

```{math}
\text{diskbbq\_ram} \approx 0.01 \text{ to } 0.05 \times (\text{centroid\_bytes} + \text{quantized\_vector\_bytes})
```

:::{tip}
Start with 5% of the DiskBBQ index structure in RAM and tune downward based on benchmark results. The required fraction depends on your query patterns — queries that access overlapping clusters benefit from caching.
:::
:::::

::::::

### Cluster-wide totals [_cluster_wide_totals]

Each shard replica holds a full copy of the vector data and index structures. To estimate cluster-wide resource requirements, multiply the per-replica estimates by the total number of copies:

```{math}
\text{total\_copies} = 1 \text{ (primary)} + \text{num\_replicas}
```

```{math}
\text{cluster\_disk} = \text{total\_disk\_per\_replica} \times \text{total\_copies}
```

```{math}
\text{cluster\_ram} = \text{total\_ram\_per\_replica} \times \text{total\_copies}
```

:::{note}
The cluster-wide RAM is spread across data nodes that hold the shard replicas. Each data node only needs enough RAM for the replicas assigned to it.
:::

### Worked examples [_sizing_worked_examples]

::::{dropdown} Example: HNSW with float vectors and no quantization
**Configuration:** 1,000,000 vectors, 1,024 dimensions, `element_type: float`, HNSW with `m = 16`, no quantization, 1 replica.

**Disk (per replica):**

```{math}
\begin{align*}
\text{raw vectors} &= 1{,}000{,}000 \times 1{,}024 \times 4 = 4{,}096{,}000{,}000 \text{ bytes} \approx 3.81 \text{ GB} \\
\text{HNSW graph} &= 1{,}000{,}000 \times 4 \times 16 = 64{,}000{,}000 \text{ bytes} \approx 61.0 \text{ MB} \\
\text{total disk} &\approx 3.87 \text{ GB}
\end{align*}
```

**Off-heap RAM (per replica):**

```{math}
\begin{align*}
\text{vector RAM} &= 4{,}096{,}000{,}000 \text{ bytes} \approx 3.81 \text{ GB} \\
\text{HNSW graph RAM} &= 64{,}000{,}000 \text{ bytes} \approx 61.0 \text{ MB} \\
\text{total RAM} &\approx 3.87 \text{ GB}
\end{align*}
```

**Cluster-wide (1 primary + 1 replica = 2 copies):** ~7.74 GB disk, ~7.74 GB RAM
::::

::::{dropdown} Example: HNSW with float vectors and int8 quantization
**Configuration:** 1,000,000 vectors, 1,024 dimensions, `element_type: float`, HNSW with `m = 16`, `int8` quantization, 1 replica.

**Disk (per replica):**

```{math}
\begin{align*}
\text{raw vectors} &= 1{,}000{,}000 \times 1{,}024 \times 4 = 4{,}096{,}000{,}000 \text{ bytes} \approx 3.81 \text{ GB} \\
\text{int8 quantized} &= 1{,}000{,}000 \times (1{,}024 + 4) = 1{,}028{,}000{,}000 \text{ bytes} \approx 980 \text{ MB} \\
\text{HNSW graph} &= 1{,}000{,}000 \times 64 = 64{,}000{,}000 \text{ bytes} \approx 61.0 \text{ MB} \\
\text{total disk} &\approx 4.83 \text{ GB}
\end{align*}
```

**Off-heap RAM (per replica):**

```{math}
\begin{align*}
\text{int8 vector RAM} &= 1{,}028{,}000{,}000 \text{ bytes} \approx 980 \text{ MB} \\
\text{HNSW graph RAM} &= 64{,}000{,}000 \text{ bytes} \approx 61.0 \text{ MB} \\
\text{total RAM} &\approx 1.02 \text{ GB}
\end{align*}
```

:::{note}
With `int8` quantization, disk increases by ~25% (both raw and quantized vectors are stored), but RAM drops from 3.87 GB to 1.02 GB — a **~4× reduction**.
:::

**Cluster-wide (1 primary + 1 replica = 2 copies):** ~9.66 GB disk, ~2.03 GB RAM
::::

::::{dropdown} Example: HNSW with float vectors and BBQ quantization
**Configuration:** 10,000,000 vectors, 768 dimensions, `element_type: float`, HNSW with `m = 16`, `bbq` quantization, 1 replica.

**Disk (per replica):**

```{math}
\begin{align*}
\text{raw vectors} &= 10{,}000{,}000 \times 768 \times 4 = 30{,}720{,}000{,}000 \text{ bytes} \approx 28.6 \text{ GB} \\
\text{BBQ quantized} &= 10{,}000{,}000 \times (96 + 14) = 1{,}100{,}000{,}000 \text{ bytes} \approx 1.02 \text{ GB} \\
\text{HNSW graph} &= 10{,}000{,}000 \times 64 = 640{,}000{,}000 \text{ bytes} \approx 596 \text{ MB} \\
\text{total disk} &\approx 30.2 \text{ GB}
\end{align*}
```

**Off-heap RAM (per replica):**

```{math}
\begin{align*}
\text{BBQ vector RAM} &= 1{,}100{,}000{,}000 \text{ bytes} \approx 1.02 \text{ GB} \\
\text{HNSW graph RAM} &= 640{,}000{,}000 \text{ bytes} \approx 596 \text{ MB} \\
\text{total RAM} &\approx 1.62 \text{ GB}
\end{align*}
```

:::{note}
BBQ quantization reduces RAM from ~29.2 GB (unquantized) to ~1.62 GB — a **~18× reduction** for 768-dimensional vectors. This is ideal for large-scale vector workloads.
:::

**Cluster-wide (1 primary + 1 replica = 2 copies):** ~60.4 GB disk, ~3.24 GB RAM
::::

::::{dropdown} Example: DiskBBQ
**Configuration:** 10,000,000 vectors, 768 dimensions, `element_type: float`, DiskBBQ with `vectors_per_cluster = 384`, 1 replica.

**Disk (per replica):**

```{math}
\begin{align*}
\text{raw vectors} &= 10{,}000{,}000 \times 768 \times 4 = 30{,}720{,}000{,}000 \text{ bytes} \approx 28.6 \text{ GB} \\
\text{num\_clusters} &= \lceil 10{,}000{,}000 / 384 \rceil = 26{,}042 \\
\text{centroid bytes} &= 26{,}042 \times 768 \times 4 + 26{,}042 \times (768 + 14) \\
&= 80{,}001{,}024 + 20{,}356{,}844 = 100{,}357{,}868 \text{ bytes} \approx 95.7 \text{ MB} \\
\text{quantized vectors} &= 10{,}000{,}000 \times ((96 + 14 + 2) \times 2) = 2{,}240{,}000{,}000 \text{ bytes} \approx 2.09 \text{ GB} \\
\text{total disk} &\approx 30.8 \text{ GB}
\end{align*}
```

**Off-heap RAM (per replica):**

```{math}
\begin{align*}
\text{DiskBBQ index size} &= 100{,}357{,}868 + 2{,}240{,}000{,}000 = 2{,}340{,}357{,}868 \text{ bytes} \\
\text{RAM at 5\%} &\approx 117{,}018{,}000 \text{ bytes} \approx 112 \text{ MB}
\end{align*}
```

:::{note}
DiskBBQ requires dramatically less RAM than HNSW — ~112 MB vs ~29.2 GB for the same 10M×768 unquantized HNSW setup. Trade-off: DiskBBQ has higher query latency since most data is read from disk.
:::

**Cluster-wide (1 primary + 1 replica = 2 copies):** ~61.5 GB disk, ~224 MB RAM
::::

### Quick-reference: RAM reduction from quantization [_quantization_ram_comparison]

The following table shows the RAM reduction factor for `float` vectors at common dimensions. All values assume HNSW with `m = 16`.

| Dimensions | No quantization | `int8` | `int4` | `bbq` |
| --- | --- | --- | --- | --- |
| 384 | 1,600 B/vec | 388 B/vec (4.1×) | 196 B/vec (8.2×) | 62 B/vec (25.8×) |
| 768 | 3,136 B/vec | 772 B/vec (4.1×) | 388 B/vec (8.1×) | 110 B/vec (28.5×) |
| 1,024 | 4,160 B/vec | 1,028 B/vec (4.0×) | 516 B/vec (8.1×) | 142 B/vec (29.3×) |
| 1,536 | 6,208 B/vec | 1,540 B/vec (4.0×) | 772 B/vec (8.0×) | 206 B/vec (30.1×) |

:::{note}
Bytes per vector include HNSW graph overhead (64 bytes with `m = 16`). Reduction factors compare total per-vector RAM (vectors + graph) against the unquantized baseline.
:::

The data nodes should also leave a buffer for other ways that RAM is needed. For example your index might also include text fields and numerics, which also benefit from using filesystem cache. It's recommended to run benchmarks with your specific dataset to ensure there's a sufficient amount of memory to give good search performance. You can find [here](https://elasticsearch-benchmarks.elastic.co/#tracks/so_vector) and [here](https://elasticsearch-benchmarks.elastic.co/#tracks/dense_vector) some examples of datasets and configurations that we use for our nightly benchmarks.
