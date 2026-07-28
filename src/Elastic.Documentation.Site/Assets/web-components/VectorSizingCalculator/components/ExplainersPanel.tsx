import { EuiAccordion, EuiCode, EuiIcon, EuiLink, EuiText } from '@elastic/eui'
import type { ReactNode } from 'react'

const KNN_MEMORY_DOC =
    'https://www.elastic.co/docs/deploy-manage/production-guidance/optimize-performance/approximate-knn-search#_ensure_data_nodes_have_enough_memory'

interface SectionProps {
    id: string
    title: string
    initialOpen?: boolean
    children: ReactNode
}

function ExplainerSection({ id, title, initialOpen, children }: SectionProps) {
    return (
        <EuiAccordion
            id={id}
            initialIsOpen={initialOpen}
            paddingSize="m"
            className="vectorSizingCalc__explainerAccordion"
            buttonContent={
                <span className="vectorSizingCalc__explainerSectionTitle">
                    {title}
                </span>
            }
        >
            <EuiText size="s" className="vectorSizingCalc__explainerBody">
                {children}
            </EuiText>
        </EuiAccordion>
    )
}

export function ExplainersPanel() {
    return (
        <div className="vectorSizingCalc__explainersPanel">
            <div className="vectorSizingCalc__explainersHeader">
                <span className="vectorSizingCalc__explainersTitle">
                    <EuiIcon type="documentation" size="m" color="inherit" />
                    <span>How it is computed</span>
                </span>
                <EuiLink href={KNN_MEMORY_DOC} target="_blank" external>
                    Tune approximate kNN search
                </EuiLink>
            </div>

            <ExplainerSection
                id="vscExplainerHeap"
                title="Heap vs off-heap — is the HNSW graph on heap?"
                initialOpen
            >
                <p>
                    <strong>No — the HNSW graph is off-heap.</strong> In modern
                    Lucene/Elasticsearch the graph is written to the{' '}
                    <EuiCode>.vex</EuiCode> file and memory-mapped, so it lives
                    in the operating system&rsquo;s filesystem cache, not the
                    JVM heap. Elasticsearch reports it under{' '}
                    <EuiCode>off_heap.total_vex_size_bytes</EuiCode> in the
                    index stats API, next to <EuiCode>vec</EuiCode>,{' '}
                    <EuiCode>veq</EuiCode>, <EuiCode>veb</EuiCode>,{' '}
                    <EuiCode>cenivf</EuiCode> and <EuiCode>clivf</EuiCode>.
                </p>
                <p>
                    <strong>All vector data structures</strong> (raw vectors,
                    quantized vectors, HNSW graph, DiskBBQ centroids &amp;
                    clusters) are memory-mapped, so they are off-heap. The{' '}
                    <strong>heap</strong> is used only transiently and does not
                    scale as a persistent, index-sized structure:
                </p>
                <ul>
                    <li>
                        Per query: neighbour/candidate priority queues sized by{' '}
                        <EuiCode>ef</EuiCode> /{' '}
                        <EuiCode>num_candidates</EuiCode>.
                    </li>
                    <li>
                        Per query: a &ldquo;visited&rdquo; bitset roughly{' '}
                        <EuiCode>segment_vectors / 8</EuiCode> bytes, per
                        concurrent query per segment.
                    </li>
                    <li>
                        During merge: transient buffers while the graph is
                        (re)built.
                    </li>
                </ul>
                <p>
                    Size heap for concurrency and merge pressure, not for
                    &ldquo;fitting the vectors&rdquo; — that job belongs to
                    off-heap RAM.
                </p>
            </ExplainerSection>

            <ExplainerSection
                id="vscExplainerAssumptions"
                title="Assumptions & what this does NOT model"
                initialOpen
            >
                <ul>
                    <li>
                        <strong>
                            Off-heap = minimum hot set for fast approximate
                            search.
                        </strong>{' '}
                        For quantized types the raw <EuiCode>.vec</EuiCode>{' '}
                        floats are excluded from &ldquo;off-heap needed&rdquo;.
                        Elasticsearch&rsquo;s off-heap stats still report{' '}
                        <EuiCode>.vec</EuiCode> (it is memory-mapped), and
                        rescoring reads some raw pages per query;{' '}
                        <EuiCode>on_disk_rescore</EuiCode> keeps them on disk.
                    </li>
                    <li>
                        <strong>
                            The <EuiCode>bbq_disk</EuiCode> off-heap band (all
                            centroids + 5–10% of posting lists)
                        </strong>{' '}
                        is a benchmark heuristic, not derived from code. Treat
                        it as workload-dependent and tune it.
                    </li>
                    <li>
                        <strong>
                            The HNSW graph <EuiCode>V × 4 × m</EuiCode> is a
                            heuristic
                        </strong>{' '}
                        (the figure the public docs use). The real{' '}
                        <EuiCode>.vex</EuiCode> is varint delta-encoded,
                        multi-level, with level 0 up to 2×m neighbours, so
                        actual size varies.
                    </li>
                    <li>
                        <strong>
                            <EuiCode>.cenivf</EuiCode> is a lower bound.
                        </strong>{' '}
                        It excludes the parent centroid layer, the
                        vector→centroid lookup table, and (ESNext) raw float
                        centroids. <EuiCode>num_centroids ≈ V / C</EuiCode> is
                        itself approximate.
                    </li>
                    <li>
                        <strong>
                            The ×2 on <EuiCode>.clivf</EuiCode> is a worst case
                        </strong>{' '}
                        (every vector gets a SOAR overspill copy). Reality is
                        between 1× and 2×.
                    </li>
                    <li>
                        <strong>Not modelled:</strong> metadata files (
                        <EuiCode>.vem*</EuiCode>), doc-id maps, deleted/updated
                        docs, codec headers/footers, and segment count — these
                        formulas describe a single merged view.
                    </li>
                    <li>
                        <strong>Units are binary</strong> (1 GiB = 1,024 MiB).
                        Disk vendors and some Cloud tools show decimal GB.
                    </li>
                    <li>
                        <strong>Current codec only.</strong> Figures assume
                        newly-indexed data on today&rsquo;s formats; upgraded
                        segments may differ (e.g. legacy int8 ={' '}
                        <EuiCode>D + 4</EuiCode>).
                    </li>
                </ul>
            </ExplainerSection>

            <ExplainerSection
                id="vscExplainerFormulas"
                title="Per-component formulas"
            >
                <p>
                    Let <EuiCode>V</EuiCode> = vectors, <EuiCode>D</EuiCode> =
                    dimensions, <EuiCode>m</EuiCode> = HNSW neighbours/node
                    (default 16), <EuiCode>C</EuiCode> = DiskBBQ vectors per
                    cluster (default 384), <EuiCode>f</EuiCode> = source element
                    bytes (float 4, bfloat16 2, byte 1, bit 1/8).
                </p>
                <ul>
                    <li>
                        <strong>Raw vectors</strong> <EuiCode>.vec</EuiCode>:{' '}
                        <EuiCode>V × D × f</EuiCode> (bit:{' '}
                        <EuiCode>V × ⌈D/8⌉</EuiCode>).
                    </li>
                    <li>
                        <strong>int8 quantized</strong> <EuiCode>.veq</EuiCode>:{' '}
                        <EuiCode>V × (D + 16)</EuiCode> — 1 byte/dim + 16-byte
                        OSQ correction.
                    </li>
                    <li>
                        <strong>int4 quantized</strong> <EuiCode>.veq</EuiCode>:{' '}
                        <EuiCode>V × (⌈D/2⌉ + 16)</EuiCode> — nibble-packed +
                        the same 16-byte correction.
                    </li>
                    <li>
                        <strong>BBQ quantized</strong> <EuiCode>.veb</EuiCode>:{' '}
                        <EuiCode>V × (⌈D/64⌉×8 + 14)</EuiCode> — 1 bit/dim
                        (padded to a multiple of 64) + 14-byte correction.
                    </li>
                    <li>
                        <strong>HNSW graph</strong> <EuiCode>.vex</EuiCode>:{' '}
                        <EuiCode>V × 4 × m</EuiCode> — heuristic (~4 bytes per
                        neighbour × m).
                    </li>
                    <li>
                        <strong>DiskBBQ centroids</strong>{' '}
                        <EuiCode>.cenivf</EuiCode>:{' '}
                        <EuiCode>(V / C) × (D + 16)</EuiCode> — 7-bit quantized
                        IVF centroids.
                    </li>
                    <li>
                        <strong>DiskBBQ clusters</strong>{' '}
                        <EuiCode>.clivf</EuiCode>:{' '}
                        <EuiCode>V × 2 × (⌈D/8⌉ + 16)</EuiCode> — 1-bit when D ≥
                        384 else 4-bit (<EuiCode>⌈D/2⌉</EuiCode>); ×2 is a
                        conservative SOAR upper bound.
                    </li>
                </ul>
                <p>
                    <strong>Why +16 vs +14 vs +4?</strong> Quantized formats
                    store an{' '}
                    <EuiCode>
                        OptimizedScalarQuantizer.QuantizationResult
                    </EuiCode>{' '}
                    per vector: 3 correction floats (12 bytes) +{' '}
                    <EuiCode>quantizedComponentSum</EuiCode>. When the sum is an{' '}
                    <EuiCode>int</EuiCode> (int8/int4 <EuiCode>.veq</EuiCode>{' '}
                    and DiskBBQ) the correction is <strong>16</strong> bytes;
                    when it is a <EuiCode>short</EuiCode> (BBQ{' '}
                    <EuiCode>.veb</EuiCode>) it is <strong>14</strong>. The
                    legacy Lucene99 scalar format stored a single 4-byte
                    correction (<EuiCode>+4</EuiCode>) — the figure still in the
                    public docs, but not what new <EuiCode>int8_*</EuiCode>/
                    <EuiCode>int4_*</EuiCode> fields write.
                </p>
            </ExplainerSection>

            <ExplainerSection
                id="vscExplainerResidency"
                title="Disk vs off-heap: what stays in RAM per index type"
            >
                <ul>
                    <li>
                        <EuiCode>flat</EuiCode>: resident = raw. disk = raw.
                    </li>
                    <li>
                        <EuiCode>hnsw</EuiCode>: resident = raw + graph. disk =
                        raw + graph.
                    </li>
                    <li>
                        <EuiCode>int8_flat</EuiCode> /{' '}
                        <EuiCode>int4_flat</EuiCode>: resident = quantized. disk
                        = raw + quantized.
                    </li>
                    <li>
                        <EuiCode>int8_hnsw</EuiCode> /{' '}
                        <EuiCode>int4_hnsw</EuiCode>: resident = quantized +
                        graph. disk = raw + quantized + graph.
                    </li>
                    <li>
                        <EuiCode>bbq_flat</EuiCode>: resident = BBQ. disk = raw
                        + BBQ.
                    </li>
                    <li>
                        <EuiCode>bbq_hnsw</EuiCode>: resident = BBQ + graph.
                        disk = raw + BBQ + graph.
                    </li>
                    <li>
                        <EuiCode>bbq_disk</EuiCode> (DiskBBQ): disk = raw +
                        centroids + clusters. Resident is a range: all centroids
                        + 5–10% of the posting lists. No HNSW graph — IVF
                        clustering pages in only touched clusters.
                    </li>
                </ul>
                <p>
                    For every quantized index the raw full-precision vectors
                    stay on disk and are read only for optional rescoring, so
                    they are not counted toward required off-heap RAM.
                </p>
            </ExplainerSection>

            <ExplainerSection
                id="vscExplainerFiles"
                title="File extension reference"
            >
                <ul>
                    <li>
                        <EuiCode>.vec</EuiCode> — raw, non-quantized vector
                        values (float / bfloat16 / byte / bit).
                    </li>
                    <li>
                        <EuiCode>.veq</EuiCode> — int4 / int8 quantized vectors.
                    </li>
                    <li>
                        <EuiCode>.veb</EuiCode> — BBQ (1-bit) quantized vectors.
                    </li>
                    <li>
                        <EuiCode>.vex</EuiCode> — the HNSW graph.
                    </li>
                    <li>
                        <EuiCode>.cenivf</EuiCode> — DiskBBQ centroids.
                    </li>
                    <li>
                        <EuiCode>.clivf</EuiCode> — DiskBBQ clusters of
                        quantized vectors.
                    </li>
                    <li>
                        <EuiCode>.vem*</EuiCode> — small metadata (not a sizing
                        concern).
                    </li>
                </ul>
            </ExplainerSection>

            <ExplainerSection
                id="vscExplainerSources"
                title="Source references (verified against Elasticsearch code)"
            >
                <p>
                    Every constant was traced to the current source under{' '}
                    <EuiCode>server/src/main/java/org/elasticsearch/…</EuiCode>.
                </p>
                <ul>
                    <li>
                        <strong>int8 / int4 +16:</strong>{' '}
                        <EuiCode>Int7uOSQVectorScorerSupplier.java:42</EuiCode>{' '}
                        (
                        <EuiCode>
                            CORRECTIONS_BYTES = 3×Float.BYTES + Integer.BYTES
                        </EuiCode>
                        ), pitch <EuiCode>dims + 16</EuiCode> at{' '}
                        <EuiCode>:57</EuiCode>; int4{' '}
                        <EuiCode>⌈D/2⌉ + 16</EuiCode> at{' '}
                        <EuiCode>Int4VectorScorerSupplier.java:49</EuiCode>.
                    </li>
                    <li>
                        <strong>BBQ +14:</strong>{' '}
                        <EuiCode>
                            ES93BinaryQuantizedVectorScorer.java:23
                        </EuiCode>{' '}
                        (
                        <EuiCode>
                            BBQ_CORRECTIONS_BYTES = 3×Float.BYTES + Short.BYTES
                        </EuiCode>
                        ); bit payload <EuiCode>discretize(D, 64)/8</EuiCode> at{' '}
                        <EuiCode>BQVectorUtils.java:44</EuiCode>.
                    </li>
                    <li>
                        <strong>DiskBBQ:</strong> centroids/clusters +16 in{' '}
                        <EuiCode>
                            ES940DiskBBQVectorsWriter.java:860-864
                        </EuiCode>
                        ; 1-bit vs 4-bit split{' '}
                        <EuiCode>DenseVectorFieldMapper.java:511</EuiCode> (
                        <EuiCode>dims &lt; 384 ? 4 : 1</EuiCode>); ×2 SOAR{' '}
                        <EuiCode>
                            ES940DiskBBQVectorsWriter.java:272-276
                        </EuiCode>
                        ;{' '}
                        <EuiCode>C = DEFAULT_VECTORS_PER_CLUSTER = 384</EuiCode>
                        .
                    </li>
                    <li>
                        <strong>Raw / HNSW:</strong> extensions in{' '}
                        <EuiCode>LuceneFilesExtensions.java:81,82</EuiCode>;
                        bfloat16 = 2 bytes <EuiCode>BFloat16.java:20</EuiCode>;
                        m = <EuiCode>DEFAULT_MAX_CONN = 16</EuiCode>.
                    </li>
                    <li>
                        <strong>Off-heap stats</strong> reported per extension
                        in <EuiCode>DenseVectorStats.java</EuiCode> via each
                        reader&rsquo;s <EuiCode>getOffHeapByteSize</EuiCode>.
                    </li>
                </ul>
            </ExplainerSection>
        </div>
    )
}
