import { parseVectorCount } from '../parseVectorCount'
import type {
    ElementType,
    IndexType,
    Quantization,
    ValidationResult,
} from '../types'
import { LabelWithTip } from './LabelWithTip'
import {
    EuiCallOut,
    EuiFieldText,
    EuiFormRow,
    EuiLink,
    EuiRange,
    EuiSelect,
    EuiSpacer,
} from '@elastic/eui'
import { useEffect, useMemo, useState } from 'react'

const VECTOR_COUNT_PRESETS = [
    { key: '1k', text: '1.000', value: 1_000 },
    { key: '10k', text: '10.000', value: 10_000 },
    { key: '100k', text: '100.000', value: 100_000 },
    { key: '1m', text: '1.000.000', value: 1_000_000 },
    { key: '10m', text: '10.000.000', value: 10_000_000 },
    { key: '50m', text: '50.000.000', value: 50_000_000 },
] as const

const VECTOR_PRESET_SELECT_OPTIONS = VECTOR_COUNT_PRESETS.map((p) => ({
    value: p.key,
    text: p.text,
}))

const DIMENSION_PRESETS = [256, 384, 512, 768, 1024, 1536, 3072, 4096] as const

const DIMENSION_SELECT_OPTIONS = [
    ...DIMENSION_PRESETS.map((n) => ({
        value: String(n),
        text: String(n),
    })),
]

const TOOLTIPS = {
    vectors:
        'The total number of vectors you plan to index, usually equal to the number of documents or chunks.',
    dimensions:
        'The length of each vector, set by your embedding model (for example 768 or 1536).',
    elementType:
        'The numeric format used to store each dimension. In most cases use float32 and quantization to reduce memory.',
    indexStructure:
        'HNSW builds a graph for fast approximate search. Flat is exact brute-force. DiskBBQ stores vectors on disk.',
    graphConnections:
        'How many neighbors each node connects to in the HNSW graph. Higher values improve recall but increase memory and build time.',
    quantization:
        'Compresses vectors to reduce memory. Options depend on element type and index structure.',
    replicas:
        'Each replica is a full copy of your index. Total copies = 1 primary + replicas.',
}

function presetKeyForVectorsText(vectorsText: string): string {
    const n = parseVectorCount(vectorsText)
    if (!vectorsText.trim() || Number.isNaN(n) || n <= 0) return 'custom'
    const hit = VECTOR_COUNT_PRESETS.find((p) => p.value === n)
    return hit ? hit.key : 'custom'
}

function dimensionPresetKey(numDimensions: number | string): string {
    if (numDimensions === '') return 'custom'
    const n =
        typeof numDimensions === 'number'
            ? numDimensions
            : Number(numDimensions)
    if (Number.isNaN(n)) return 'custom'
    return DIMENSION_PRESETS.includes(n as (typeof DIMENSION_PRESETS)[number])
        ? String(n)
        : 'custom'
}

const ELEMENT_TYPE_OPTIONS: { value: ElementType; text: string }[] = [
    { value: 'float', text: 'Full precision (float32)' },
    { value: 'bfloat16', text: 'Half precision (bfloat16)' },
    { value: 'byte', text: 'Byte compressed (int8)' },
    { value: 'bit', text: 'Binary compressed (bit)' },
]

interface ConfigurationPanelProps {
    vectorsText: string
    onVectorsChange: (value: string) => void
    numDimensions: number | string
    onDimensionsChange: (value: number | string) => void
    elementType: ElementType
    onElementTypeChange: (value: ElementType) => void
    indexType: IndexType
    onIndexTypeChange: (value: IndexType) => void
    indexTypeOptions: { value: string; text: string }[]
    quantization: Quantization
    onQuantizationChange: (value: Quantization) => void
    quantOptions: { value: string; label: string }[]
    replicas: number
    onReplicasChange: (value: number) => void
    hnswM: number
    onHnswMChange: (value: number) => void
    validation: ValidationResult
}

export function ConfigurationPanel({
    vectorsText,
    onVectorsChange,
    numDimensions,
    onDimensionsChange,
    elementType,
    onElementTypeChange,
    indexType,
    onIndexTypeChange,
    indexTypeOptions,
    quantization,
    onQuantizationChange,
    quantOptions,
    replicas,
    onReplicasChange,
    hnswM,
    onHnswMChange,
    validation,
}: ConfigurationPanelProps) {
    const derivedVectorsPresetKey = useMemo(
        () => presetKeyForVectorsText(vectorsText),
        [vectorsText]
    )
    const vectorsPresetSelectValue = VECTOR_COUNT_PRESETS.some(
        (preset) => preset.key === derivedVectorsPresetKey
    )
        ? derivedVectorsPresetKey
        : VECTOR_PRESET_SELECT_OPTIONS[0].value

    const derivedDimPresetKey = useMemo(
        () => dimensionPresetKey(numDimensions),
        [numDimensions]
    )
    const dimensionsPresetSelectValue = DIMENSION_PRESETS.includes(
        Number(derivedDimPresetKey) as (typeof DIMENSION_PRESETS)[number]
    )
        ? derivedDimPresetKey
        : String(DIMENSION_PRESETS[0])

    const showHnswSlider = indexType === 'hnsw'
    const showQuantizationControl = quantOptions.length > 1
    const [hnswMText, setHnswMText] = useState(String(hnswM))
    const [isHnswMEditing, setIsHnswMEditing] = useState(false)
    const [replicasText, setReplicasText] = useState(() => String(replicas))
    const [isReplicasEditing, setIsReplicasEditing] = useState(false)

    useEffect(() => {
        if (isHnswMEditing) return
        setHnswMText(String(hnswM))
    }, [hnswM, isHnswMEditing])

    useEffect(() => {
        if (isReplicasEditing) return
        setReplicasText(String(replicas))
    }, [replicas, isReplicasEditing])

    const normalizeHnswM = (raw: string) => {
        const trimmed = raw.trim()
        if (!trimmed) return 2

        const n = Number(trimmed.replace(/[^\d]/g, ''))
        if (Number.isNaN(n)) return 2

        const rounded = Math.round(n)
        const clamped = Math.min(512, Math.max(2, rounded))
        // Keep values aligned with the slider step (2).
        if (clamped % 2 === 0) return clamped
        const down = clamped - 1
        const up = clamped + 1
        if (down < 2) return 2
        if (up > 512) return 510
        return Math.abs(n - down) <= Math.abs(up - n) ? down : up
    }

    const normalizeReplicas = (raw: string) => {
        const trimmed = raw.trim()
        if (trimmed === '') return 1
        const n = Number(trimmed.replace(/[^\d]/g, ''))
        if (Number.isNaN(n)) return 1
        return Math.min(99, Math.max(1, n))
    }

    const replicasFormRow = (
        <EuiFormRow
            label={
                <LabelWithTip tip={TOOLTIPS.replicas}>Replicas</LabelWithTip>
            }
        >
            <EuiFieldText
                compressed
                fullWidth
                inputMode="numeric"
                pattern="[0-9]*"
                value={replicasText}
                onFocus={() => setIsReplicasEditing(true)}
                onChange={(e) => {
                    const cleaned = e.target.value.replace(/[^\d]/g, '')
                    setReplicasText(cleaned)
                }}
                onBlur={() => {
                    const next = normalizeReplicas(replicasText)
                    setReplicasText(String(next))
                    onReplicasChange(next)
                    setIsReplicasEditing(false)
                }}
            />
        </EuiFormRow>
    )

    const hnswMInputWidthPx = useMemo(() => {
        const digits = hnswMText.length
        // Rough-but-stable sizing: enough room for digits + control chrome.
        // Tuned for 1–3 digit values (2..512) while still feeling "tight".
        const base = 34
        const perDigit = 10
        const min = 44
        const max = 72
        return Math.min(
            max,
            Math.max(min, base + perDigit * Math.max(1, digits))
        )
    }, [hnswMText])

    return (
        <div className="vectorSizingCalc__panel vectorSizingCalc__panel--left">
            <div className="vectorSizingCalc__sectionTitle">Vectors</div>

            <EuiFormRow
                label={
                    <LabelWithTip tip={TOOLTIPS.vectors}>
                        Number of vectors
                    </LabelWithTip>
                }
            >
                <EuiSelect
                    options={VECTOR_PRESET_SELECT_OPTIONS}
                    value={vectorsPresetSelectValue}
                    aria-label="Number of vectors"
                    onChange={(e) => {
                        const preset = VECTOR_COUNT_PRESETS.find(
                            (p) => p.key === e.target.value
                        )
                        if (preset) {
                            onVectorsChange(
                                preset.value.toLocaleString('en-US')
                            )
                        }
                    }}
                />
            </EuiFormRow>

            <EuiFormRow
                className="vectorSizingCalc__row--dimensions"
                label={
                    <LabelWithTip tip={TOOLTIPS.dimensions}>
                        Dimensions
                    </LabelWithTip>
                }
            >
                <EuiSelect
                    options={DIMENSION_SELECT_OPTIONS}
                    value={dimensionsPresetSelectValue}
                    aria-label="Dimensions"
                    onChange={(e) => {
                        onDimensionsChange(Number(e.target.value))
                    }}
                />
            </EuiFormRow>

            <EuiSpacer size="l" />
            <div className="vectorSizingCalc__sectionDivider" />
            <EuiSpacer size="l" />

            <div className="vectorSizingCalc__sectionTitle">Index options</div>

            <EuiFormRow
                label={
                    <LabelWithTip tip={TOOLTIPS.elementType}>
                        Element type
                    </LabelWithTip>
                }
            >
                <EuiSelect
                    options={ELEMENT_TYPE_OPTIONS}
                    value={elementType}
                    onChange={(e) =>
                        onElementTypeChange(e.target.value as ElementType)
                    }
                />
            </EuiFormRow>

            <EuiSpacer size="s" />

            <EuiFormRow
                label={
                    <LabelWithTip tip={TOOLTIPS.indexStructure}>
                        Index structure
                    </LabelWithTip>
                }
            >
                <EuiSelect
                    options={indexTypeOptions}
                    value={indexType}
                    onChange={(e) =>
                        onIndexTypeChange(e.target.value as IndexType)
                    }
                />
            </EuiFormRow>

            {showHnswSlider && (
                <>
                    <EuiSpacer size="s" />
                    <div className="vectorSizingCalc__graphConnectionsBlock">
                        <div className="vectorSizingCalc__graphConnectionsLabel">
                            <LabelWithTip tip={TOOLTIPS.graphConnections}>
                                Graph connections (m)
                            </LabelWithTip>
                        </div>
                        <div className="vectorSizingCalc__graphConnectionsControlsRow">
                            <div className="vectorSizingCalc__graphConnectionsSlider">
                                <EuiRange
                                    value={hnswM}
                                    min={2}
                                    max={512}
                                    step={2}
                                    showInput={false}
                                    showLabels={false}
                                    showRange
                                    fullWidth
                                    onChange={(e) =>
                                        onHnswMChange(Number(e.target.value))
                                    }
                                    aria-label="HNSW m"
                                />
                            </div>
                            <div className="vectorSizingCalc__graphConnectionsValueCell">
                                <EuiFieldText
                                    compressed
                                    inputMode="numeric"
                                    pattern="[0-9]*"
                                    value={hnswMText}
                                    style={{ width: `${hnswMInputWidthPx}px` }}
                                    onFocus={() => setIsHnswMEditing(true)}
                                    onChange={(e) => {
                                        const cleaned = e.target.value.replace(
                                            /[^\d]/g,
                                            ''
                                        )
                                        setHnswMText(cleaned)
                                    }}
                                    onBlur={() => {
                                        const next = normalizeHnswM(hnswMText)
                                        setHnswMText(String(next))
                                        onHnswMChange(next)
                                        setIsHnswMEditing(false)
                                    }}
                                />
                            </div>
                        </div>
                    </div>
                </>
            )}

            <EuiSpacer size="m" />

            {showQuantizationControl ? (
                <div className="vectorSizingCalc__fieldGrid2">
                    <EuiFormRow
                        label={
                            <LabelWithTip tip={TOOLTIPS.quantization}>
                                Quantization
                            </LabelWithTip>
                        }
                    >
                        <EuiSelect
                            options={quantOptions.map((o) => ({
                                value: o.value,
                                text: o.label,
                            }))}
                            value={quantization}
                            disabled={indexType === 'disk_bbq'}
                            onChange={(e) =>
                                onQuantizationChange(
                                    e.target.value as Quantization
                                )
                            }
                        />
                    </EuiFormRow>

                    {replicasFormRow}
                </div>
            ) : (
                replicasFormRow
            )}

            {validation.warning && (
                <>
                    <EuiSpacer size="m" />
                    <EuiCallOut
                        title={validation.warning}
                        color="danger"
                        iconType="alert"
                        size="s"
                    >
                        {validation.warningLink && (
                            <EuiLink href={validation.warningLink}>
                                See documentation
                            </EuiLink>
                        )}
                    </EuiCallOut>
                </>
            )}
        </div>
    )
}
