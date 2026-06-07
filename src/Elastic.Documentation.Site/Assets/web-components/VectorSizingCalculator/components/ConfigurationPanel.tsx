import {
    clampDiskBbqOffHeapPercent,
    DISKBBQ_OFF_HEAP_RAM_MAX_PERCENT,
    DISKBBQ_OFF_HEAP_RAM_MIN_PERCENT,
} from '../calculations'
import {
    formatGroupedInteger,
    normalizeGroupedNumberInput,
} from '../formatNumbers'
import { parseVectorCount } from '../parseVectorCount'
import type {
    ElementType,
    IndexType,
    Quantization,
    ValidationResult,
} from '../types'
import { LabelWithTip } from './LabelWithTip'
import {
    EuiButtonEmpty,
    EuiCallOut,
    EuiComboBox,
    type EuiComboBoxOptionOption,
    EuiFieldNumber,
    EuiFormRow,
    EuiLink,
    EuiRange,
    EuiSelect,
    EuiSpacer,
} from '@elastic/eui'
import { useMemo, useState } from 'react'

const VECTOR_COUNT_PRESET_VALUES = [
    { key: '1k', value: 1_000 },
    { key: '10k', value: 10_000 },
    { key: '100k', value: 100_000 },
    { key: '1m', value: 1_000_000 },
    { key: '10m', value: 10_000_000 },
    { key: '50m', value: 50_000_000 },
] as const

const VECTOR_COUNT_PRESETS = VECTOR_COUNT_PRESET_VALUES.map((preset) => ({
    ...preset,
    text: formatGroupedInteger(preset.value),
}))

const VECTOR_COMBO_OPTIONS: EuiComboBoxOptionOption<string>[] =
    VECTOR_COUNT_PRESETS.map((p) => ({
        label: p.text,
        value: p.key,
    }))

const DIMENSION_PRESETS = [256, 384, 512, 768, 1024, 1536, 3072, 4096] as const

const DIMENSION_COMBO_OPTIONS: EuiComboBoxOptionOption<string>[] =
    DIMENSION_PRESETS.map((n) => ({
        label: String(n),
        value: String(n),
    }))

const TOOLTIPS = {
    vectors:
        'Count every vector you will store in the index, not just documents. One document can produce multiple vectors. For example, one embedding per product image plus one for the description.',
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
        'Number of replica shards (not including the primary). Total index copies = 1 primary + replica shards.',
    vectorsPerCluster:
        'For DiskBBQ, vectors are grouped into clusters. This value sets how many vectors each cluster holds and affects cluster count and disk use.',
    offHeapRam:
        'Share of quantized vectors kept in off-heap RAM. Lower values save memory; higher values improve query throughput and latency (centroids are always fully in RAM).',
    hnswIndexStructure: 'RAM estimates include the HNSW graph.',
    diskBbqIndexStructure: 'RAM estimates reflect off-heap cache.',
}

function presetKeyForVectorsText(vectorsText: string): string {
    const n = parseVectorCount(vectorsText)
    if (!vectorsText.trim() || Number.isNaN(n) || n <= 0) return 'custom'
    const hit = VECTOR_COUNT_PRESETS.find((p) => p.value === n)
    return hit ? hit.key : 'custom'
}

function formatVectorsTextFromInput(raw: string): string {
    const n = parseVectorCount(raw)
    if (!Number.isNaN(n) && n > 0) {
        return formatGroupedInteger(n)
    }
    return raw.trim()
}

function vectorsTextToComboSelection(
    vectorsText: string
): EuiComboBoxOptionOption<string>[] {
    const presetKey = presetKeyForVectorsText(vectorsText)
    if (presetKey !== 'custom') {
        const preset = VECTOR_COUNT_PRESETS.find((p) => p.key === presetKey)
        if (preset) {
            return [{ label: preset.text, value: preset.key }]
        }
    }
    const trimmed = vectorsText.trim()
    if (!trimmed) return []
    return [{ label: trimmed, value: 'custom' }]
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

function parseDimensionsInput(raw: string): number | '' {
    const trimmed = normalizeGroupedNumberInput(raw)
    if (!trimmed) return ''
    const n = Math.round(Number(trimmed))
    if (Number.isNaN(n) || n <= 0) return ''
    return Math.min(4096, Math.max(1, n))
}

function dimensionsToComboSelection(
    numDimensions: number | string
): EuiComboBoxOptionOption<string>[] {
    const presetKey = dimensionPresetKey(numDimensions)
    if (presetKey !== 'custom') {
        return [{ label: presetKey, value: presetKey }]
    }
    if (numDimensions === '') return []
    const label =
        typeof numDimensions === 'number'
            ? String(numDimensions)
            : String(numDimensions).trim()
    if (!label) return []
    return [{ label, value: 'custom' }]
}

const ELEMENT_TYPE_OPTIONS: { value: ElementType; text: string }[] = [
    { value: 'float', text: 'float32' },
    { value: 'bfloat16', text: 'bfloat16' },
    { value: 'byte', text: 'int8' },
    { value: 'bit', text: 'bit' },
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
    vectorsPerCluster: number
    onVectorsPerClusterChange: (value: number) => void
    offHeapRamPercent: number
    onOffHeapRamPercentChange: (value: number) => void
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
    vectorsPerCluster,
    onVectorsPerClusterChange,
    offHeapRamPercent,
    onOffHeapRamPercentChange,
    validation,
}: ConfigurationPanelProps) {
    const vectorsComboSelection = useMemo(
        () => vectorsTextToComboSelection(vectorsText),
        [vectorsText]
    )

    const dimensionsComboSelection = useMemo(
        () => dimensionsToComboSelection(numDimensions),
        [numDimensions]
    )

    const showHnswSlider = indexType === 'hnsw'
    const showOffHeapRamSlider = indexType === 'disk_bbq'
    const showVectorsPerCluster = indexType === 'disk_bbq'
    const showQuantizationControl = quantOptions.length > 1
    const [advancedOpen, setAdvancedOpen] = useState(false)

    const clampHnswM = (value: number) => {
        if (Number.isNaN(value)) return 2
        const rounded = Math.round(value)
        const clamped = Math.min(512, Math.max(2, rounded))
        if (clamped % 2 === 0) return clamped
        const down = clamped - 1
        const up = clamped + 1
        if (down < 2) return 2
        if (up > 512) return 510
        return Math.abs(value - down) <= Math.abs(up - value) ? down : up
    }

    const clampVectorsPerCluster = (value: number) => {
        if (Number.isNaN(value)) return 384
        return Math.min(1_000_000, Math.max(1, Math.round(value)))
    }

    const clampReplicas = (value: number) => {
        if (Number.isNaN(value)) return 0
        return Math.min(99, Math.max(0, Math.round(value)))
    }

    const replicaShardsFormRow = (
        <EuiFormRow
            fullWidth
            label={
                <LabelWithTip tip={TOOLTIPS.replicas}>
                    Replica shards
                </LabelWithTip>
            }
        >
            <EuiFieldNumber
                fullWidth
                value={replicas}
                min={0}
                max={99}
                step={1}
                onChange={(e) =>
                    onReplicasChange(clampReplicas(Number(e.target.value)))
                }
            />
        </EuiFormRow>
    )

    return (
        <div className="vectorSizingCalc__panel vectorSizingCalc__panel--left">
            <div className="vectorSizingCalc__sectionTitle">Vectors</div>

            <EuiFormRow
                fullWidth
                label={
                    <LabelWithTip tip={TOOLTIPS.vectors}>
                        Number of vectors
                    </LabelWithTip>
                }
            >
                <EuiComboBox
                    fullWidth
                    singleSelection={{ asPlainText: true }}
                    options={VECTOR_COMBO_OPTIONS}
                    selectedOptions={vectorsComboSelection}
                    aria-label="Number of vectors"
                    placeholder="e.g. 1,000,000"
                    onChange={(selected) => {
                        if (selected.length === 0) {
                            onVectorsChange('')
                            return
                        }
                        const option = selected[0]
                        const preset = VECTOR_COUNT_PRESETS.find(
                            (p) => p.key === option.value
                        )
                        if (preset) {
                            onVectorsChange(formatGroupedInteger(preset.value))
                            return
                        }
                        onVectorsChange(
                            formatVectorsTextFromInput(option.label)
                        )
                    }}
                    onCreateOption={(searchValue) => {
                        onVectorsChange(formatVectorsTextFromInput(searchValue))
                    }}
                />
            </EuiFormRow>

            <EuiFormRow
                fullWidth
                className="vectorSizingCalc__row--dimensions"
                label={
                    <LabelWithTip tip={TOOLTIPS.dimensions}>
                        Dimensions
                    </LabelWithTip>
                }
            >
                <EuiComboBox
                    fullWidth
                    singleSelection={{ asPlainText: true }}
                    options={DIMENSION_COMBO_OPTIONS}
                    selectedOptions={dimensionsComboSelection}
                    aria-label="Dimensions"
                    placeholder="e.g. 768"
                    onChange={(selected) => {
                        if (selected.length === 0) {
                            onDimensionsChange('')
                            return
                        }
                        const option = selected[0]
                        const preset = DIMENSION_PRESETS.find(
                            (n) => String(n) === option.value
                        )
                        if (preset !== undefined) {
                            onDimensionsChange(preset)
                            return
                        }
                        onDimensionsChange(parseDimensionsInput(option.label))
                    }}
                    onCreateOption={(searchValue) => {
                        onDimensionsChange(parseDimensionsInput(searchValue))
                    }}
                />
            </EuiFormRow>

            <EuiSpacer size="m" />

            <EuiButtonEmpty
                className="vectorSizingCalc__sectionToggle"
                flush="left"
                iconType={
                    advancedOpen ? 'chevronSingleUp' : 'chevronSingleDown'
                }
                iconSide="right"
                onClick={() => setAdvancedOpen((open) => !open)}
            >
                {advancedOpen
                    ? 'Hide advanced settings'
                    : 'Show advanced settings'}
            </EuiButtonEmpty>

            {advancedOpen && (
                <>
                    <EuiSpacer size="l" />

                    <EuiFormRow
                        fullWidth
                        label={
                            <LabelWithTip tip={TOOLTIPS.elementType}>
                                Element type
                            </LabelWithTip>
                        }
                    >
                        <EuiSelect
                            fullWidth
                            options={ELEMENT_TYPE_OPTIONS}
                            value={elementType}
                            onChange={(e) =>
                                onElementTypeChange(
                                    e.target.value as ElementType
                                )
                            }
                        />
                    </EuiFormRow>

                    <EuiSpacer size="s" />

                    <EuiFormRow
                        fullWidth
                        label={
                            <LabelWithTip tip={TOOLTIPS.indexStructure}>
                                Index structure
                            </LabelWithTip>
                        }
                        helpText={
                            indexType === 'hnsw'
                                ? TOOLTIPS.hnswIndexStructure
                                : indexType === 'disk_bbq'
                                  ? TOOLTIPS.diskBbqIndexStructure
                                  : undefined
                        }
                    >
                        <EuiSelect
                            fullWidth
                            options={indexTypeOptions}
                            value={indexType}
                            onChange={(e) =>
                                onIndexTypeChange(e.target.value as IndexType)
                            }
                        />
                    </EuiFormRow>

                    {showOffHeapRamSlider && (
                        <>
                            <EuiSpacer size="s" />
                            <div className="vectorSizingCalc__graphConnectionsBlock">
                                <div className="vectorSizingCalc__graphConnectionsLabel">
                                    <LabelWithTip tip={TOOLTIPS.offHeapRam}>
                                        % allocated to off-heap RAM
                                    </LabelWithTip>
                                </div>
                                <div className="vectorSizingCalc__graphConnectionsControlsRow">
                                    <div className="vectorSizingCalc__graphConnectionsSlider">
                                        <EuiRange
                                            compressed={false}
                                            value={offHeapRamPercent}
                                            min={
                                                DISKBBQ_OFF_HEAP_RAM_MIN_PERCENT
                                            }
                                            max={
                                                DISKBBQ_OFF_HEAP_RAM_MAX_PERCENT
                                            }
                                            step={1}
                                            showInput={false}
                                            showLabels={false}
                                            showRange
                                            fullWidth
                                            onChange={(e) =>
                                                onOffHeapRamPercentChange(
                                                    clampDiskBbqOffHeapPercent(
                                                        Number(
                                                            (
                                                                e.currentTarget as HTMLInputElement
                                                            ).value
                                                        )
                                                    )
                                                )
                                            }
                                            aria-label="Percent allocated to off-heap RAM"
                                        />
                                    </div>
                                    <div className="vectorSizingCalc__graphConnectionsValueCell">
                                        <EuiFieldNumber
                                            className="vectorSizingCalc__rangeNumberInput"
                                            value={offHeapRamPercent}
                                            min={
                                                DISKBBQ_OFF_HEAP_RAM_MIN_PERCENT
                                            }
                                            max={
                                                DISKBBQ_OFF_HEAP_RAM_MAX_PERCENT
                                            }
                                            step={1}
                                            aria-label="Percent allocated to off-heap RAM"
                                            onChange={(e) => {
                                                const next =
                                                    clampDiskBbqOffHeapPercent(
                                                        Number(e.target.value)
                                                    )
                                                onOffHeapRamPercentChange(next)
                                            }}
                                        />
                                    </div>
                                </div>
                            </div>
                        </>
                    )}

                    {showHnswSlider && (
                        <>
                            <EuiSpacer size="s" />
                            <div className="vectorSizingCalc__graphConnectionsBlock">
                                <div className="vectorSizingCalc__graphConnectionsLabel">
                                    <LabelWithTip
                                        tip={TOOLTIPS.graphConnections}
                                    >
                                        Graph connections (m)
                                    </LabelWithTip>
                                </div>
                                <div className="vectorSizingCalc__graphConnectionsControlsRow">
                                    <div className="vectorSizingCalc__graphConnectionsSlider">
                                        <EuiRange
                                            compressed={false}
                                            value={hnswM}
                                            min={2}
                                            max={512}
                                            step={2}
                                            showInput={false}
                                            showLabels={false}
                                            showRange
                                            fullWidth
                                            onChange={(e) =>
                                                onHnswMChange(
                                                    Number(
                                                        (
                                                            e.currentTarget as HTMLInputElement
                                                        ).value
                                                    )
                                                )
                                            }
                                            aria-label="HNSW m"
                                        />
                                    </div>
                                    <div className="vectorSizingCalc__graphConnectionsValueCell">
                                        <EuiFieldNumber
                                            className="vectorSizingCalc__rangeNumberInput"
                                            value={hnswM}
                                            min={2}
                                            max={512}
                                            step={2}
                                            aria-label="HNSW m"
                                            onChange={(e) =>
                                                onHnswMChange(
                                                    clampHnswM(
                                                        Number(e.target.value)
                                                    )
                                                )
                                            }
                                        />
                                    </div>
                                </div>
                            </div>
                        </>
                    )}

                    {showVectorsPerCluster && (
                        <>
                            <EuiSpacer size="s" />
                            <EuiFormRow
                                fullWidth
                                label={
                                    <LabelWithTip
                                        tip={TOOLTIPS.vectorsPerCluster}
                                    >
                                        Vectors per cluster
                                    </LabelWithTip>
                                }
                            >
                                <EuiFieldNumber
                                    fullWidth
                                    value={vectorsPerCluster}
                                    min={1}
                                    max={1_000_000}
                                    step={1}
                                    onChange={(e) =>
                                        onVectorsPerClusterChange(
                                            clampVectorsPerCluster(
                                                Number(e.target.value)
                                            )
                                        )
                                    }
                                />
                            </EuiFormRow>
                        </>
                    )}

                    <EuiSpacer size="m" />

                    {showQuantizationControl ? (
                        <div className="vectorSizingCalc__fieldGrid2">
                            <EuiFormRow
                                fullWidth
                                label={
                                    <LabelWithTip tip={TOOLTIPS.quantization}>
                                        Quantization
                                    </LabelWithTip>
                                }
                            >
                                <EuiSelect
                                    fullWidth
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

                            {replicaShardsFormRow}
                        </div>
                    ) : (
                        replicaShardsFormRow
                    )}
                </>
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
