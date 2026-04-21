import {
    EuiCallOut,
    EuiIcon,
    EuiSpacer,
    EuiText,
} from '@elastic/eui';
import type { Quantization, SizingResult, ValidationResult } from '../types';
import { formatBytesString } from '../calculations';

interface ResultsPanelProps {
    result: SizingResult | null;
    inputsValid: boolean;
    quantizationLabel: string;
    replicas: number;
    quantization: Quantization;
    validation: ValidationResult;
    quantizationInsightText: string | null;
}

const QUANTIZATION_CALLOUT_FALLBACK =
    'Quantization reduces storage usage compared to full-precision vectors for the same logical dataset.';

function formatRamHero(bytes: number): string {
    const gb = bytes / 1024 ** 3;
    return gb >= 10 ? `${Math.round(gb)} GB` : `${gb.toFixed(1)} GB`;
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
    const showBody = inputsValid && result !== null;
    const infoText =
        validation.note ??
        quantizationInsightText ??
        (quantization !== 'none' ? QUANTIZATION_CALLOUT_FALLBACK : '');

    const showInfoCallout =
        showBody &&
        !validation.warning &&
        (Boolean(validation.note) ||
            Boolean(quantizationInsightText) ||
            quantization !== 'none');
    const ctaClassName = showBody
        ? 'vectorSizingCalc__ctaGroup'
        : 'vectorSizingCalc__ctaGroup vectorSizingCalc__ctaGroup--centered';

    return (
        <div className="vectorSizingCalc__panel vectorSizingCalc__panel--right">
            <div className="vectorSizingCalc__summary">
                <EuiText size="s" className="vectorSizingCalc__summaryLabel">
                    RAM required:
                </EuiText>
                <EuiSpacer size="xs" className="vectorSizingCalc__spacerHeroGap" />
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
                        <strong>
                            {showBody ? String(replicas) : '0'}
                        </strong>
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
                        <EuiCallOut className="vectorSizingCalc__infoCallout" color="success" size="s" iconType={undefined}>
                            <div className="vectorSizingCalc__infoCalloutRow">
                                <EuiIcon type="info" className="vectorSizingCalc__infoCalloutIcon" />
                                <span className="vectorSizingCalc__infoCalloutText">{infoText}</span>
                            </div>
                        </EuiCallOut>
                    </>
                )}

            </div>
            <div className={ctaClassName}>
                <a
                    href="https://cloud.elastic.co/registration?page=docs&placement=docs-siderail"
                    className="vectorSizingCalc__ctaPrimary w-full min-h-[30px] cursor-pointer text-white text-nowrap bg-blue-elastic hover:bg-blue-elastic-110 focus:ring-4 focus:outline-none focus:ring-blue-elastic-50 font-semibold font-sans rounded-sm px-6 py-2 text-center flex items-center justify-center leading-[30px]"
                >
                    Get started free
                </a>
                <EuiSpacer size="s" className="vectorSizingCalc__spacerCtaToContact" />
                <div className="vectorSizingCalc__contactWrap">
                    <a
                        className="vectorSizingCalc__contactLink"
                        href="https://www.elastic.co/contact"
                    >
                        Contact us
                    </a>
                </div>
            </div>
        </div>
    );
}
