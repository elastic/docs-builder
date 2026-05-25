import { formatGroupedInteger } from './formatNumbers'
import type {
    CalculatorInputs,
    SizingResult,
    ValidationResult,
    BreakdownItem,
} from './types'

/** Return the number of raw bytes per vector based on element type + dimensions. */
function rawBytesPerVector(elementType: string, D: number): number {
    switch (elementType) {
        case 'float':
            return D * 4
        case 'bfloat16':
            return D * 2
        case 'byte':
            return D
        case 'bit':
            return Math.ceil(D / 8)
        default:
            return D * 4
    }
}

/** Human-readable byte formatting. */
export function formatBytes(bytes: number): { value: string; unit: string } {
    if (bytes === 0) return { value: '0', unit: 'bytes' }
    const units = ['bytes', 'KB', 'MB', 'GB', 'TB', 'PB']
    let idx = 0
    let val = bytes
    while (val >= 1024 && idx < units.length - 1) {
        val /= 1024
        idx++
    }
    const formatted =
        val < 10 ? val.toFixed(2) : val < 100 ? val.toFixed(1) : val.toFixed(0)
    return { value: formatted, unit: units[idx] }
}

export function formatBytesString(bytes: number): string {
    const f = formatBytes(bytes)
    return `${f.value} ${f.unit}`
}

/** DiskBBQ off-heap slider: min/max % of quantized vectors to cache in RAM. */
export const DISKBBQ_OFF_HEAP_RAM_MIN_PERCENT = 0
export const DISKBBQ_OFF_HEAP_RAM_MAX_PERCENT = 100

export function clampDiskBbqOffHeapPercent(percent: number): number {
    return Math.min(
        DISKBBQ_OFF_HEAP_RAM_MAX_PERCENT,
        Math.max(DISKBBQ_OFF_HEAP_RAM_MIN_PERCENT, Math.round(percent))
    )
}

/** Returns the list of available quantization options for a given element type + index type. */
export function getAvailableQuantizations(
    elementType: string,
    indexType: string
): { value: string; label: string }[] {
    if (indexType === 'disk_bbq') {
        return [{ value: 'bbq', label: 'BBQ (built-in)' }]
    }
    const options: { value: string; label: string }[] = [
        { value: 'none', label: 'None' },
    ]
    if (elementType === 'float' || elementType === 'bfloat16') {
        options.push(
            { value: 'int8', label: 'int8' },
            { value: 'int4', label: 'int4' },
            { value: 'bbq', label: 'BBQ' }
        )
    }
    return options
}

/** Validate inputs and return any warnings. */
export function validate(inputs: CalculatorInputs): ValidationResult {
    const { numVectors, numDimensions, elementType, quantization, indexType } =
        inputs

    if (
        isNaN(numVectors) ||
        isNaN(numDimensions) ||
        numVectors <= 0 ||
        numDimensions <= 0
    ) {
        return { valid: false }
    }

    if (numDimensions > 4096) {
        return {
            valid: false,
            warning:
                'Elasticsearch supports a maximum of 4,096 dimensions for dense_vector fields.',
            warningLink:
                'https://www.elastic.co/docs/reference/elasticsearch/mapping-reference/dense-vector#dense-vector-params',
        }
    }

    if (
        (elementType === 'byte' || elementType === 'bit') &&
        quantization !== 'none' &&
        indexType !== 'disk_bbq'
    ) {
        return {
            valid: true,
            warning: `Quantization is not applicable to ${elementType} element type.`,
        }
    }

    if (
        elementType === 'float' &&
        numDimensions >= 384 &&
        quantization === 'none' &&
        indexType !== 'disk_bbq'
    ) {
        return {
            valid: true,
            note: 'For float vectors with dimensions ≥ 384, Elastic strongly recommends using a quantized index to reduce memory footprint.',
        }
    }

    return { valid: true }
}

/** Compute all sizing estimates. */
export function calculate(inputs: CalculatorInputs): SizingResult | null {
    const {
        numVectors: V,
        numDimensions: D,
        elementType,
        indexType,
        quantization,
        replicas,
        hnswM: m,
        vectorsPerCluster: vpc,
        offHeapRamPercent,
    } = inputs

    if (isNaN(V) || isNaN(D) || V <= 0 || D <= 0) return null
    if (D > 4096) return null

    const diskFormulas: string[] = []
    const ramFormulas: string[] = []
    const clusterFormulas: string[] = []

    // --- Disk ---

    // Raw vectors
    const rpv = rawBytesPerVector(elementType, D)
    const rawDisk = V * rpv
    diskFormulas.push(
        `Raw vectors on disk = ${formatGroupedInteger(V)} × ${rpv} = ${formatBytesString(rawDisk)}`
    )

    // Quantized vectors (disk)
    let quantDisk = 0
    let quantLabel = ''
    if (indexType !== 'disk_bbq') {
        switch (quantization) {
            case 'int8':
                quantDisk = V * (D + 4)
                quantLabel = 'int8 quantized vectors'
                diskFormulas.push(
                    `int8 quantized = V × (D + 4) = ${formatBytesString(quantDisk)}`
                )
                break
            case 'int4':
                quantDisk = V * (Math.ceil(D / 2) + 4)
                quantLabel = 'int4 quantized vectors'
                diskFormulas.push(
                    `int4 quantized = V × (⌈D/2⌉ + 4) = ${formatBytesString(quantDisk)}`
                )
                break
            case 'bbq':
                quantDisk = V * (Math.ceil(D / 8) + 14)
                quantLabel = 'BBQ quantized vectors'
                diskFormulas.push(
                    `BBQ quantized = V × (⌈D/8⌉ + 14) = ${formatBytesString(quantDisk)}`
                )
                break
        }
    }

    // Index structure (disk)
    let indexDisk = 0
    let indexLabel = ''
    let bbqCentroids = 0
    let bbqVectors = 0

    if (indexType === 'hnsw') {
        indexDisk = V * 4 * m
        indexLabel = 'HNSW graph'
        diskFormulas.push(
            `HNSW graph = V × 4 × ${m} = ${formatBytesString(indexDisk)}`
        )
    } else if (indexType === 'disk_bbq') {
        const nc = Math.ceil(V / vpc)
        bbqCentroids = nc * D * 4 + nc * (D + 14)
        bbqVectors = V * ((Math.ceil(D / 8) + 14 + 2) * 2)
        indexDisk = bbqCentroids + bbqVectors
        indexLabel = 'DiskBBQ structures'
        diskFormulas.push(
            `DiskBBQ clusters = ⌈V / ${vpc}⌉ = ${formatGroupedInteger(nc)}`
        )
        diskFormulas.push(`Centroid bytes = ${formatBytesString(bbqCentroids)}`)
        diskFormulas.push(
            `Quantized cluster vectors = ${formatBytesString(bbqVectors)}`
        )
    }

    const totalDisk = rawDisk + quantDisk + indexDisk
    diskFormulas.push(
        `Total disk (per replica) = ${formatBytesString(totalDisk)}`
    )

    // Build disk breakdown
    const diskBreakdown: BreakdownItem[] = [
        { label: 'Raw vectors', bytes: rawDisk, color: 'primary' },
    ]
    if (quantDisk > 0)
        diskBreakdown.push({
            label: quantLabel,
            bytes: quantDisk,
            color: 'accent',
        })
    if (indexDisk > 0)
        diskBreakdown.push({
            label: indexLabel,
            bytes: indexDisk,
            color: 'warning',
        })

    // --- RAM ---
    let ramVectors = 0
    let ramVectorsMax = 0
    let ramVectorsLabel = ''
    let ramIndex = 0
    let ramIndexLabel = ''

    if (indexType === 'hnsw' || indexType === 'flat') {
        switch (quantization) {
            case 'none':
                ramVectors = rawDisk
                ramVectorsLabel = 'Raw vectors in RAM'
                ramFormulas.push(`Vector RAM = ${formatBytesString(ramVectors)}`)
                break
            case 'int8':
                ramVectors = V * (D + 4)
                ramVectorsLabel = 'int8 vectors in RAM'
                ramFormulas.push(
                    `Vector RAM (int8) = ${formatBytesString(ramVectors)}`
                )
                break
            case 'int4':
                ramVectors = V * (Math.ceil(D / 2) + 4)
                ramVectorsLabel = 'int4 vectors in RAM'
                ramFormulas.push(
                    `Vector RAM (int4) = ${formatBytesString(ramVectors)}`
                )
                break
            case 'bbq':
                ramVectors = V * (Math.ceil(D / 8) + 14)
                ramVectorsLabel = 'BBQ vectors in RAM'
                ramFormulas.push(
                    `Vector RAM (BBQ) = ${formatBytesString(ramVectors)}`
                )
                break
        }
        if (indexType === 'hnsw') {
            ramIndex = V * 4 * m
            ramIndexLabel = 'HNSW graph in RAM'
            ramFormulas.push(`HNSW graph RAM = ${formatBytesString(ramIndex)}`)
        }
    } else if (indexType === 'disk_bbq') {
        const pct = clampDiskBbqOffHeapPercent(offHeapRamPercent) / 100
        const ramMin = bbqCentroids
        const ramMax = bbqCentroids + bbqVectors
        const ramSelected = bbqCentroids + Math.ceil(bbqVectors * pct)
        ramVectors = ramSelected
        ramVectorsMax = ramMax
        ramVectorsLabel = 'DiskBBQ off-heap RAM'
        ramFormulas.push(
            `DiskBBQ RAM min = centroids + 0% × vectors = ${formatBytesString(ramMin)}`
        )
        ramFormulas.push(
            `DiskBBQ RAM max = centroids + 100% × vectors = ${formatBytesString(ramMax)}`
        )
        ramFormulas.push(
            `DiskBBQ RAM (${clampDiskBbqOffHeapPercent(offHeapRamPercent)}% vectors cached) = centroids + ${clampDiskBbqOffHeapPercent(offHeapRamPercent)}% × vectors = ${formatBytesString(ramSelected)}`
        )
        ramFormulas.push(
            '  Centroids fully in RAM; cached vector fraction depends on throughput/latency goals'
        )
    }

    const usesRamRange = false
    const totalRam = ramVectors + ramIndex
    const totalRamMin =
        indexType === 'disk_bbq' ? bbqCentroids + ramIndex : totalRam
    const totalRamMax =
        indexType === 'disk_bbq' ? ramVectorsMax + ramIndex : totalRam
    ramFormulas.push(
        `Total off-heap RAM (per replica) = ${formatBytesString(totalRam)}`
    )

    // Build RAM breakdown
    const ramBreakdown: BreakdownItem[] = []
    if (ramVectors > 0)
        ramBreakdown.push({
            label: ramVectorsLabel,
            bytes: ramVectors,
            color: 'primary',
        })
    if (ramIndex > 0)
        ramBreakdown.push({
            label: ramIndexLabel,
            bytes: ramIndex,
            color: 'accent',
        })

    // Cluster totals
    const totalCopies = 1 + replicas
    const clusterDisk = totalDisk * totalCopies
    const clusterRam = totalRam * totalCopies
    const clusterRamMax = totalRamMax * totalCopies
    const clusterRamMin = totalRamMin * totalCopies

    if (replicas > 0) {
        clusterFormulas.push(
            `Cluster total disk = per-replica × ${totalCopies} copies = ${formatBytesString(clusterDisk)}`
        )
        clusterFormulas.push(
            `Cluster total RAM  = per-replica × ${totalCopies} copies = ${formatBytesString(clusterRam)}`
        )
    }

    return {
        diskBreakdown,
        ramBreakdown,
        totalDisk,
        totalRam,
        totalRamMin,
        totalRamMax,
        clusterDisk,
        clusterRam,
        clusterRamMin,
        clusterRamMax,
        usesRamRange,
        totalCopies,
        formulas: {
            disk: diskFormulas,
            ram: ramFormulas,
            cluster: clusterFormulas,
        },
    }
}

/** Hero-style RAM label; supports DiskBBQ min–max range. */
export function formatRamHeroLabel(minBytes: number, maxBytes: number): string {
    if (minBytes <= 0 && maxBytes <= 0) return '0 MB'
    if (minBytes === maxBytes) return formatRamHeroSingle(minBytes)

    const minLabel = formatRamHeroSingle(minBytes)
    const maxLabel = formatRamHeroSingle(maxBytes)
    const minUnit = minLabel.split(' ').pop()
    const maxUnit = maxLabel.split(' ').pop()
    if (minUnit === maxUnit) {
        const minValue = minLabel.slice(0, -(minUnit!.length + 1))
        const maxValue = maxLabel.slice(0, -(maxUnit!.length + 1))
        return `${minValue}–${maxValue} ${minUnit}`
    }
    return `${minLabel} – ${maxLabel}`
}

function formatRamHeroSingle(bytes: number): string {
    if (bytes <= 0) return '0 MB'
    const gb = bytes / 1024 ** 3
    if (gb >= 1) {
        return gb >= 10 ? `${Math.round(gb)} GB` : `${gb.toFixed(1)} GB`
    }
    const mb = bytes / 1024 ** 2
    return mb >= 10 ? `${Math.round(mb)} MB` : `${mb.toFixed(1)} MB`
}

/** KV-row byte label for min–max RAM. */
export function formatBytesRangeLabel(
    minBytes: number,
    maxBytes: number
): string {
    if (minBytes === maxBytes) return formatBytesString(minBytes)
    return `${formatBytesString(minBytes)} – ${formatBytesString(maxBytes)}`
}

/**
 * Dynamic callout copy: same inputs with quantization set to `none` vs current.
 * The "~N" value is cluster RAM saved when positive (RAM is what drops with
 * quantization in this model). If cluster disk were lower with quantization,
 * that delta would be used instead.
 */
export function getQuantizationInsightText(
    inputs: CalculatorInputs,
    current: SizingResult
): string | null {
    const { quantization, indexType, elementType } = inputs
    if (quantization === 'none') return null
    if (indexType === 'disk_bbq') return null
    if (elementType !== 'float' && elementType !== 'bfloat16') return null

    const baseline = calculate({ ...inputs, quantization: 'none' })
    if (!baseline) return null

    const ramSave = baseline.clusterRam - current.clusterRam
    if (ramSave > 0) {
        return `Quantization reduces memory usage, saving ~${formatBytesString(
            ramSave
        )} compared to full-precision vectors.`
    }

    const diskSave = baseline.clusterDisk - current.clusterDisk
    if (diskSave > 0) {
        return `Quantization reduces storage usage, saving ~${formatBytesString(
            diskSave
        )} compared to full-precision vectors.`
    }

    return null
}
