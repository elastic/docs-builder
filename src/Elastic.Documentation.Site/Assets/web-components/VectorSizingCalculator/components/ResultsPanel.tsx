import { formatBytesString } from '../calculations'
import { formatDiskToRamSentence, formatGroupedInteger } from '../formatNumbers'
import type { SizingResult, ValidationResult } from '../types'
import { CalcToolTip } from './CalcToolTip'
import { HeroSizeLine } from './HeroSizeLine'
import { EuiHorizontalRule, EuiLink, EuiText } from '@elastic/eui'

interface ResultsPanelProps {
    result: SizingResult | null
    inputsValid: boolean
    quantizationLabel: string
    replicas: number
    validation: ValidationResult
}

const KNN_MEMORY_DOC =
    'https://www.elastic.co/docs/deploy-manage/production-guidance/optimize-performance/approximate-knn-search#_ensure_data_nodes_have_enough_memory'

const SIZING_DISCLAIMER =
    'These estimates are vector-field disk and off-heap (page cache) RAM per copy, for self-managed Elasticsearch and Elastic Cloud Hosted. They are not JVM heap, not a full-node size, and not how you size Elastic Cloud Serverless. Actual needs still depend on data shape, indexing settings, and query patterns.'

const DISK_TO_RAM_TIP =
    'This is how much disk this field uses relative to its off-heap RAM working set. A high ratio, typical of DiskBBQ, means most of the index can stay on disk. It is not a node type, not JVM heap, and not a serverless capacity number.'

function clusterResourcesLabel(replicas: number): string {
    if (replicas === 0) {
        return 'Cluster total (primary only):'
    }
    if (replicas === 1) {
        return 'Cluster total (1 primary + 1 replica):'
    }
    return `Cluster total (1 primary + ${formatGroupedInteger(replicas)} replicas):`
}

export function ResultsPanel({
    result,
    inputsValid,
    quantizationLabel,
    replicas,
    validation,
}: ResultsPanelProps) {
    const showBody = inputsValid && result !== null && !validation.warning
    const diskToRamSentence =
        showBody && result ? formatDiskToRamSentence(result.diskToRamRatio) : ''

    return (
        <div className="vectorSizingCalc__panel vectorSizingCalc__panel--right">
            <div className="vectorSizingCalc__summary">
                <div className="vectorSizingCalc__summaryMain">
                    <EuiText
                        size="s"
                        className="vectorSizingCalc__summaryLabel"
                    >
                        {showBody
                            ? clusterResourcesLabel(replicas)
                            : 'Cluster total:'}
                    </EuiText>

                    {showBody ? (
                        <div className="vectorSizingCalc__heroTotals">
                            <HeroSizeLine
                                bytes={result.clusterDisk}
                                resourceLabel="Disk"
                            />
                            <HeroSizeLine
                                bytes={result.clusterRam}
                                resourceLabel="Off-heap RAM"
                            />
                        </div>
                    ) : (
                        <div className="vectorSizingCalc__heroTotals">
                            <HeroSizeLine bytes={0} resourceLabel="Disk" />
                            <HeroSizeLine
                                bytes={0}
                                resourceLabel="Off-heap RAM"
                            />
                        </div>
                    )}

                    <EuiHorizontalRule margin="l" />

                    <div className="vectorSizingCalc__resultsDetail">
                        <div className="vectorSizingCalc__resultsDetailLabels">
                            <EuiText
                                size="s"
                                className="vectorSizingCalc__detailLabel"
                            >
                                Index options type:
                            </EuiText>
                            <EuiText
                                size="s"
                                className="vectorSizingCalc__detailLabel"
                            >
                                Quantization:
                            </EuiText>
                            <EuiText
                                size="s"
                                className="vectorSizingCalc__detailLabel"
                            >
                                Disk per replica:
                            </EuiText>
                            <EuiText
                                size="s"
                                className="vectorSizingCalc__detailLabel"
                            >
                                Off-heap RAM per replica:
                            </EuiText>
                        </div>
                        <div className="vectorSizingCalc__resultsDetailValues">
                            <EuiText
                                size="s"
                                className="vectorSizingCalc__detailValue"
                            >
                                {showBody && result
                                    ? result.indexOptionsType
                                    : '-'}
                            </EuiText>
                            <EuiText
                                size="s"
                                className="vectorSizingCalc__detailValue"
                            >
                                {quantizationLabel || 'None'}
                            </EuiText>
                            <EuiText
                                size="s"
                                className="vectorSizingCalc__detailValue"
                            >
                                {showBody
                                    ? formatBytesString(result.totalDisk)
                                    : '0 MiB'}
                            </EuiText>
                            <EuiText
                                size="s"
                                className="vectorSizingCalc__detailValue"
                            >
                                {showBody
                                    ? formatBytesString(result.totalRam)
                                    : '0 MiB'}
                            </EuiText>
                        </div>
                    </div>

                    {diskToRamSentence && (
                        <EuiText
                            size="s"
                            className="vectorSizingCalc__diskRamSentence"
                        >
                            <CalcToolTip
                                content={DISK_TO_RAM_TIP}
                                position="left"
                                repositionOnScroll
                            >
                                {diskToRamSentence}
                            </CalcToolTip>
                        </EuiText>
                    )}

                    <EuiHorizontalRule margin="l" />

                    <EuiText
                        size="xs"
                        className="vectorSizingCalc__binaryPrefixNote"
                    >
                        Sizes use binary units (1 GiB = 1,024 MiB).
                    </EuiText>
                </div>

                {showBody && (
                    <div className="vectorSizingCalc__disclaimerFooter">
                        <EuiText
                            size="xs"
                            className="vectorSizingCalc__disclaimer"
                        >
                            {SIZING_DISCLAIMER}{' '}
                            <EuiLink
                                href={KNN_MEMORY_DOC}
                                target="_blank"
                                external
                            >
                                Learn more
                            </EuiLink>
                        </EuiText>
                    </div>
                )}
            </div>
        </div>
    )
}
