import { EuiCode, EuiIcon, EuiLink, EuiText } from '@elastic/eui'
import { useState, type KeyboardEvent, type ReactNode } from 'react'

const KNN_MEMORY_DOC =
    'https://www.elastic.co/docs/deploy-manage/production-guidance/optimize-performance/approximate-knn-search#_ensure_data_nodes_have_enough_memory'

interface SectionProps {
    title: string
    initialOpen?: boolean
    children: ReactNode
}

function ExplainerSection({
    title,
    initialOpen = false,
    children,
}: SectionProps) {
    const [isOpen, setIsOpen] = useState(initialOpen)
    const toggle = () => setIsOpen((open) => !open)
    const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault()
            toggle()
        }
    }

    return (
        <div className="vectorSizingCalc__explainerAccordion">
            <div
                className="vectorSizingCalc__explainerSectionHeader"
                role="button"
                tabIndex={0}
                aria-expanded={isOpen}
                onClick={toggle}
                onKeyDown={onKeyDown}
            >
                <EuiIcon type={isOpen ? 'arrowDown' : 'arrowRight'} size="s" />
                <span className="vectorSizingCalc__explainerSectionTitle">
                    {title}
                </span>
            </div>
            {isOpen && (
                <EuiText size="s" className="vectorSizingCalc__explainerBody">
                    {children}
                </EuiText>
            )}
        </div>
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
                title="Disk and RAM are two different numbers"
                initialOpen
            >
                <p>
                    Sizing a <EuiCode>dense_vector</EuiCode> field comes down to
                    two figures, and they can differ by a lot:
                </p>
                <ul>
                    <li>
                        <strong>Disk</strong> - every structure persisted for
                        the field: the raw vectors, any quantized copies, and
                        the search structure (HNSW graph or DiskBBQ clusters).
                    </li>
                    <li>
                        <strong>Off-heap RAM</strong> - the working set that
                        must stay in the operating system&rsquo;s filesystem
                        cache for fast, stable query latency. Vector data is
                        memory-mapped, so it lives in the OS page cache,
                        separate from the Java heap.
                    </li>
                </ul>
                <p>
                    Provision at least the off-heap RAM figure per copy, plus
                    headroom. Once the working set no longer fits in cache,
                    queries start reading from disk and latency climbs sharply.
                    For quantized indexes the raw vectors stay on disk (read
                    only for optional rescoring), so they count toward disk but
                    not toward the required RAM.
                </p>
            </ExplainerSection>

            <ExplainerSection
                title="What must stay in RAM, per index type"
                initialOpen
            >
                <ul>
                    <li>
                        <EuiCode>flat</EuiCode> - the raw vectors.
                    </li>
                    <li>
                        <EuiCode>hnsw</EuiCode> - the raw vectors and the graph.
                    </li>
                    <li>
                        <EuiCode>int8_flat</EuiCode> /{' '}
                        <EuiCode>int4_flat</EuiCode> /{' '}
                        <EuiCode>bbq_flat</EuiCode> - the quantized codes only.
                    </li>
                    <li>
                        <EuiCode>int8_hnsw</EuiCode> /{' '}
                        <EuiCode>int4_hnsw</EuiCode> /{' '}
                        <EuiCode>bbq_hnsw</EuiCode> - the quantized codes and
                        the graph.
                    </li>
                    <li>
                        <EuiCode>bbq_disk</EuiCode> (DiskBBQ) - all centroids
                        plus a small fraction (5-10%) of the posting lists. The
                        rest of the postings and the raw vectors stay on disk,
                        and only clusters a query touches are paged in. This is
                        why DiskBBQ can serve far more vectors per GiB of RAM.
                    </li>
                </ul>
                <p>
                    Quantization keeps the raw vectors on disk but only needs
                    the much smaller codes in memory - so disk barely changes
                    across quantization types while required RAM drops sharply.
                </p>
            </ExplainerSection>

            <ExplainerSection title="How each size is calculated">
                <p>
                    With <EuiCode>V</EuiCode> vectors, <EuiCode>D</EuiCode>{' '}
                    dimensions, <EuiCode>m</EuiCode> graph connections per node
                    (default 16), <EuiCode>C</EuiCode> DiskBBQ vectors per
                    cluster (default 384), and <EuiCode>f</EuiCode> bytes per
                    element (float 4, bfloat16 2, byte 1, bit 1/8):
                </p>
                <ul>
                    <li>
                        <strong>HNSW and flat indexes</strong> - total on disk =
                        raw vectors + quantized codes (if any) + graph (HNSW
                        only):
                        <ul>
                            <li>
                                <strong>Raw vectors</strong> (always kept):{' '}
                                <EuiCode>V × D × f</EuiCode> (bit:{' '}
                                <EuiCode>V × ⌈D/8⌉</EuiCode>).
                            </li>
                            <li>
                                <strong>Quantization: none</strong> - no extra
                                codes; search uses the raw vectors.
                            </li>
                            <li>
                                <strong>Quantization: int8</strong>:{' '}
                                <EuiCode>V × (D + 16)</EuiCode> - 1 byte per
                                dimension plus a small 16-byte correction.
                            </li>
                            <li>
                                <strong>Quantization: int4</strong>:{' '}
                                <EuiCode>V × (⌈D/2⌉ + 16)</EuiCode> - half a
                                byte per dimension plus the 16-byte correction.
                            </li>
                            <li>
                                <strong>Quantization: BBQ</strong>:{' '}
                                <EuiCode>V × (⌈D/64⌉×8 + 14)</EuiCode> - 1 bit
                                per dimension (padded up to a multiple of 64)
                                plus a 14-byte correction.
                            </li>
                            <li>
                                <strong>HNSW graph</strong> (hnsw only):{' '}
                                <EuiCode>V × 4 × m</EuiCode> - the planning
                                estimate from the docs; the real graph is
                                compressed and varies.
                            </li>
                        </ul>
                    </li>
                    <li>
                        <strong>DiskBBQ (bbq_disk)</strong> - total on disk =
                        raw vectors + centroids + clusters:
                        <ul>
                            <li>
                                <strong>Raw vectors</strong> (always kept):{' '}
                                <EuiCode>V × D × f</EuiCode>.
                            </li>
                            <li>
                                <strong>Centroids</strong>:{' '}
                                <EuiCode>(V / C) × (D + 16)</EuiCode>.
                            </li>
                            <li>
                                <strong>Clusters</strong>:{' '}
                                <EuiCode>V × 2 × (⌈D/8⌉ + 16)</EuiCode> - 1-bit
                                codes when D ≥ 384, otherwise 4-bit (
                                <EuiCode>⌈D/2⌉</EuiCode>); the ×2 covers vectors
                                that spill into a second cluster.
                            </li>
                        </ul>
                    </li>
                </ul>
                <p>
                    The 14- or 16-byte &ldquo;correction&rdquo; is a small
                    per-vector value the quantizer stores so it can rescore
                    accurately.
                </p>
            </ExplainerSection>

            <ExplainerSection title="Assumptions & limitations">
                <ul>
                    <li>
                        These are estimates. Real usage depends on your data,
                        indexing settings, query patterns, merges, deletes, and
                        rescoring options.
                    </li>
                    <li>
                        Raw vectors are always kept, even for quantized indexes
                        (they are needed for rescoring).
                    </li>
                    <li>
                        The HNSW graph size (<EuiCode>V × 4 × m</EuiCode>) is a
                        planning heuristic; the stored graph is compressed and
                        varies with segment size.
                    </li>
                    <li>
                        The DiskBBQ RAM band (all centroids + 5-10% of posting
                        lists) is benchmark-based - validate it against your
                        workload.
                    </li>
                    <li>Sizes use binary units (1 GiB = 1,024 MiB).</li>
                    <li>
                        Figures assume newly-indexed data on current
                        Elasticsearch codecs.
                    </li>
                </ul>
            </ExplainerSection>
        </div>
    )
}
