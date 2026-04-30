import { formatBytesString } from '../calculations'
import type { Quantization, SizingResult, ValidationResult } from '../types'
import { EuiCallOut, EuiIcon, EuiSpacer, EuiText } from '@elastic/eui'

interface ResultsPanelProps {
    result: SizingResult | null
    inputsValid: boolean
    quantizationLabel: string
    replicas: number
    quantization: Quantization
    validation: ValidationResult
    quantizationInsightText: string | null
}

const QUANTIZATION_CALLOUT_FALLBACK =
    'Quantization reduces storage usage compared to full-precision vectors for the same logical dataset.'

function formatRamHero(bytes: number): string {
    if (bytes <= 0) return '0 MB'
    const gb = bytes / 1024 ** 3
    if (gb >= 1) {
        return gb >= 10 ? `${Math.round(gb)} GB` : `${gb.toFixed(1)} GB`
    }
    const mb = bytes / 1024 ** 2
    return mb >= 10 ? `${Math.round(mb)} MB` : `${mb.toFixed(1)} MB`
}

export function ResultsPanel({
    result,
    inputsValid,
    quantizationLabel,
    replicas,
    quantization,
    validation,
    quantizationInsightText,
}: ResultsPanelProps) {
    const showBody = inputsValid && result !== null
    const infoText =
        validation.note ??
        quantizationInsightText ??
        (quantization !== 'none' ? QUANTIZATION_CALLOUT_FALLBACK : '')

    const showInfoCallout =
        showBody &&
        !validation.warning &&
        (Boolean(validation.note) ||
            Boolean(quantizationInsightText) ||
            quantization !== 'none')

    return (
        <div className="vectorSizingCalc__panel vectorSizingCalc__panel--right">
            <div className="vectorSizingCalc__summary">
                <EuiText size="s" className="vectorSizingCalc__summaryLabel">
                    RAM required:
                </EuiText>
                <EuiSpacer
                    size="xs"
                    className="vectorSizingCalc__spacerHeroGap"
                />
                <div className="vectorSizingCalc__heroRam">
                    {showBody
                        ? formatRamHero(result.totalRam)
                        : formatRamHero(0)}
                </div>

                <EuiSpacer size="m" />

                <div className="vectorSizingCalc__kvList">
                    <div className="vectorSizingCalc__kvRow">
                        <span>Disk:</span>
                        <strong>
                            {showBody
                                ? formatBytesString(result.totalDisk)
                                : formatBytesString(0)}
                        </strong>
                    </div>
                    <div className="vectorSizingCalc__kvRow">
                        <span>Quantization:</span>
                        <strong>{quantizationLabel || 'None'}</strong>
                    </div>
                    <div className="vectorSizingCalc__kvRow">
                        <span>Replicas:</span>
                        <strong>{showBody ? String(replicas) : '0'}</strong>
                    </div>
                    <div className="vectorSizingCalc__kvRow">
                        <span>Total RAM required:</span>
                        <strong>
                            {showBody
                                ? formatBytesString(result.clusterRam)
                                : formatBytesString(0)}
                        </strong>
                    </div>
                    <div className="vectorSizingCalc__kvRow">
                        <span>Total disk required:</span>
                        <strong>
                            {showBody
                                ? formatBytesString(result.clusterDisk)
                                : formatBytesString(0)}
                        </strong>
                    </div>
                </div>

                {showInfoCallout && infoText && (
                    <>
                        <EuiSpacer size="m" />
                        <EuiCallOut
                            className="vectorSizingCalc__infoCallout"
                            color="success"
                            size="s"
                            iconType={undefined}
                        >
                            <div className="vectorSizingCalc__infoCalloutRow">
                                <EuiIcon
                                    type="info"
                                    className="vectorSizingCalc__infoCalloutIcon"
                                />
                                <span className="vectorSizingCalc__infoCalloutText">
                                    {infoText}
                                </span>
                            </div>
                        </EuiCallOut>
                    </>
                )}
            </div>
        </div>
    )
}
