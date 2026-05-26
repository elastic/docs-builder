import { formatBytesRangeLabel, formatBytesString } from '../calculations'
import { formatGroupedInteger } from '../formatNumbers'
import type { SizingResult, ValidationResult } from '../types'
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
    'This calculator is a basic approximation of per-replica and cluster disk and RAM. Real requirements depend on data shape, indexing settings, query patterns, and how you deploy Elasticsearch.'

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
                                bytes={
                                    result.usesRamRange
                                        ? undefined
                                        : result.clusterRam
                                }
                                bytesMin={
                                    result.usesRamRange
                                        ? result.clusterRamMin
                                        : undefined
                                }
                                bytesMax={
                                    result.usesRamRange
                                        ? result.clusterRamMax
                                        : undefined
                                }
                                resourceLabel="RAM"
                            />
                        </div>
                    ) : (
                        <div className="vectorSizingCalc__heroTotals">
                            <HeroSizeLine bytes={0} resourceLabel="Disk" />
                            <HeroSizeLine bytes={0} resourceLabel="RAM" />
                        </div>
                    )}

                    <EuiHorizontalRule margin="l" />

                    <div className="vectorSizingCalc__resultsDetail">
                        <div className="vectorSizingCalc__resultsDetailLabels">
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
                                RAM per replica:
                            </EuiText>
                        </div>
                        <div className="vectorSizingCalc__resultsDetailValues">
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
                                    : '0 MB'}
                            </EuiText>
                            <EuiText
                                size="s"
                                className="vectorSizingCalc__detailValue"
                            >
                                {showBody
                                    ? result.usesRamRange
                                        ? formatBytesRangeLabel(
                                              result.totalRamMin,
                                              result.totalRamMax
                                          )
                                        : formatBytesString(result.totalRam)
                                    : '0 MB'}
                            </EuiText>
                        </div>
                    </div>

                    <EuiHorizontalRule margin="l" />

                    <EuiText
                        size="xs"
                        className="vectorSizingCalc__binaryPrefixNote"
                    >
                        Sizes use binary units (1 GB = 1,024 MB).
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
                                More info
                            </EuiLink>
                        </EuiText>
                    </div>
                )}
            </div>
        </div>
    )
}
