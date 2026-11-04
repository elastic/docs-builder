import { formatBytesString } from '../calculations'
import { formatDiskToRamSentence, formatGroupedInteger } from '../formatNumbers'
import type { SizingResult, ValidationResult } from '../types'
import { CalcToolTip } from './CalcToolTip'
import { HeroSizeLine } from './HeroSizeLine'
import { EuiHorizontalRule, EuiText } from '@elastic/eui'

interface ResultsPanelProps {
    result: SizingResult | null
    inputsValid: boolean
    quantizationLabel: string
    replicas: number
    validation: ValidationResult
}

const SERVERLESS_NOTE = 'Does not apply to Elastic Cloud Serverless.'

const SIZING_DISCLAIMER =
    'These estimates are disk and RAM per copy for self-managed Elasticsearch and Elastic Cloud Hosted. This RAM is for fast search, not the Java heap, and is not a full node size. Actual needs still depend on data shape, indexing settings, and query patterns.'

const DISK_TO_RAM_TIP =
    'This compares how much the field stores on disk with how much RAM it wants in the filesystem cache for fast search. A high number, typical of DiskBBQ, means most of the index can stay on disk. It is not a node type, not Java heap, and not a serverless capacity number.'

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
                                RAM per replica:
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
                            className="vectorSizingCalc__serverlessNote"
                        >
                            {SERVERLESS_NOTE}
                        </EuiText>
                        <EuiText
                            size="xs"
                            className="vectorSizingCalc__disclaimer"
                        >
                            {SIZING_DISCLAIMER}
                        </EuiText>
                    </div>
                )}
            </div>
        </div>
    )
}
