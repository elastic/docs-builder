import { formatGroupedInteger } from './formatNumbers'
import type {
    CalculatorInputs,
    SizingResult,
    ValidationResult,
    BreakdownItem,
    ComponentRow,
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

/**
 * Quantized code + correction bytes per vector for hnsw/flat indexes (.veq/.veb).
 *   int8 = D + 16, int4 = ⌈D/2⌉ + 16   (3 floats + 1 int correction = 16 B)
 *   bbq  = ⌈D/64⌉×8 + 14                (bits padded to a multiple of 64; 3 floats + 1 short = 14 B)
 * Verified against Elasticsearch Int7uOSQVectorScorerSupplier (D + 16),
 * Int4VectorScorerSupplier (⌈D/2⌉ + 16), ES93BinaryQuantizedVectorScorer (+14),
 * and BQVectorUtils.discretize(D, 64). The legacy Lucene99 +4 correction is not
 * what current int8_* / int4_* fields write.
 */
function quantizedBytesPerVector(quantization: string, D: number): number {
    switch (quantization) {
        case 'int8':
            return D + 16
        case 'int4':
            return Math.ceil(D / 2) + 16
        case 'bbq':
            return Math.ceil(D / 64) * 8 + 14
        default:
            return 0
    }
}

/** Human-readable byte formatting. */
export function formatBytes(bytes: number): { value: string; unit: string } {
    if (bytes === 0) return { value: '0', unit: 'bytes' }
    const units = ['bytes', 'KiB', 'MiB', 'GiB', 'TiB', 'PiB']
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

/** DiskBBQ off-heap slider: % of posting lists (cluster vectors) to cache in RAM (centroids are always fully resident). */
export const DISKBBQ_OFF_HEAP_RAM_MIN_PERCENT = 0
export const DISKBBQ_OFF_HEAP_RAM_MAX_PERCENT = 10

/**
 * DiskBBQ cluster vectors use 1-bit codes at/above this dimension and 4-bit
 * (⌈D/2⌉) below it (Elasticsearch DenseVectorFieldMapper: dims < 384 ? 4 : 1).
 */
const DISKBBQ_ONE_BIT_MIN_DIMS = 384

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

/**
 * Canonical Elasticsearch `index_options.type` for the selected structure +
 * quantization, e.g. `hnsw`, `int8_flat`, `bbq_hnsw`, `bbq_disk`.
 */
export function deriveIndexOptionsType(
    indexType: string,
    quantization: string
): string {
    if (indexType === 'disk_bbq') return 'bbq_disk'
    if (quantization === 'none') return indexType
    return `${quantization}_${indexType}`
}

/** Source-precision bytes-per-dimension label for the raw `.vec` formula. */
function elementBytesLabel(elementType: string): string {
    switch (elementType) {
        case 'bfloat16':
            return '2'
        case 'byte':
            return '1'
        case 'bit':
            return '1/8'
        default:
            return '4'
    }
}

/**
 * Per-structure breakdown (per replica), mirroring the files Elasticsearch
 * writes and reports under `off_heap.*_size_bytes`. The sum of `bytes` equals
 * `totalDisk`; `offHeap` marks what must stay in the filesystem cache.
 */
function buildComponents(inputs: CalculatorInputs): ComponentRow[] {
    const {
        numVectors: V,
        numDimensions: D,
        elementType,
        indexType,
        quantization,
        hnswM: m,
        vectorsPerCluster: vpc,
    } = inputs

    const int = formatGroupedInteger
    const rows: ComponentRow[] = []

    // Raw vectors (.vec) - always written, kept for rescoring.
    const rpv = rawBytesPerVector(elementType, D)
    const rawResident = quantization === 'none' && indexType !== 'disk_bbq'
    rows.push({
        name: 'Raw vectors',
        ext: 'vec',
        formula:
            elementType === 'bit'
                ? 'V × ⌈D/8⌉'
                : `V × D × ${elementBytesLabel(elementType)}`,
        detail: `${int(V)} × ${rpv}`,
        bytes: V * rpv,
        offHeap: rawResident ? 'yes' : 'no',
        description: rawResident
            ? 'Full-precision vectors, scanned directly during search.'
            : 'Full-precision vectors; kept on disk, read only for optional rescoring.',
    })

    // Quantized vectors (hnsw / flat only).
    if (indexType !== 'disk_bbq' && quantization === 'int8') {
        rows.push({
            name: 'int8 quantized vectors',
            ext: 'veq',
            formula: 'V × (D + 16)',
            detail: `${int(V)} × (${int(D)} + 16)`,
            bytes: V * quantizedBytesPerVector('int8', D),
            offHeap: 'yes',
            description:
                '1 byte/dim + a 16-byte OSQ correction (3 floats + int component sum).',
        })
    } else if (indexType !== 'disk_bbq' && quantization === 'int4') {
        rows.push({
            name: 'int4 quantized vectors',
            ext: 'veq',
            formula: 'V × (⌈D/2⌉ + 16)',
            detail: `${int(V)} × (${int(Math.ceil(D / 2))} + 16)`,
            bytes: V * quantizedBytesPerVector('int4', D),
            offHeap: 'yes',
            description:
                '0.5 byte/dim (nibble-packed) + the same 16-byte OSQ correction.',
        })
    } else if (indexType !== 'disk_bbq' && quantization === 'bbq') {
        const packed = Math.ceil(D / 64) * 8
        rows.push({
            name: 'BBQ quantized vectors',
            ext: 'veb',
            formula: 'V × (⌈D/64⌉×8 + 14)',
            detail: `${int(V)} × (${int(packed)} + 14)`,
            bytes: V * quantizedBytesPerVector('bbq', D),
            offHeap: 'yes',
            description:
                '1 bit/dim (D padded up to a multiple of 64) + 14-byte correction (3 floats + short).',
        })
    }

    // HNSW graph (.vex).
    if (indexType === 'hnsw') {
        rows.push({
            name: 'HNSW graph',
            ext: 'vex',
            formula: 'V × 4 × m',
            detail: `${int(V)} × 4 × ${int(m)}`,
            bytes: V * 4 * m,
            offHeap: 'yes',
            description:
                'Proximity graph, ~4 bytes per neighbour × m. Heuristic; the real .vex is varint delta-encoded and multi-level. Memory-mapped → off-heap.',
        })
    }

    // DiskBBQ centroids (.cenivf) + clusters (.clivf).
    if (indexType === 'disk_bbq') {
        const nc = Math.ceil(V / vpc)
        rows.push({
            name: 'DiskBBQ centroids',
            ext: 'cenivf',
            formula: '⌈V / C⌉ × (D + 16)',
            detail: `${int(nc)} × (${int(D)} + 16)`,
            bytes: nc * (D + 16),
            offHeap: 'yes',
            description:
                '7-bit OSQ centroids (1 byte/dim) + 16-byte correction. Lower bound: excludes the parent centroid layer and vector→centroid lookup table.',
        })
        const oneBit = D >= DISKBBQ_ONE_BIT_MIN_DIMS
        const codeBytes = oneBit ? Math.ceil(D / 8) : Math.ceil(D / 2)
        rows.push({
            name: 'DiskBBQ clusters',
            ext: 'clivf',
            formula: oneBit ? 'V × 2 × (⌈D/8⌉ + 16)' : 'V × 2 × (⌈D/2⌉ + 16)',
            detail: `${int(V)} × 2 × (${int(codeBytes)} + 16)`,
            bytes: V * 2 * (codeBytes + 16),
            offHeap: 'partial',
            description: `${oneBit ? '1' : '4'}-bit OSQ vectors + 16-byte correction; D≥384 uses 1-bit, else 4-bit. The ×2 is a conservative SOAR-overspill upper bound. Only touched clusters are paged in.`,
        })
    }

    return rows
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

    // --- Disk ---
    const rpv = rawBytesPerVector(elementType, D)
    const rawDisk = V * rpv

    // Quantized vectors (disk)
    let quantDisk = 0
    let quantLabel = ''
    if (indexType !== 'disk_bbq') {
        switch (quantization) {
            case 'int8':
                quantDisk = V * quantizedBytesPerVector('int8', D)
                quantLabel = 'int8 quantized vectors'
                break
            case 'int4':
                quantDisk = V * quantizedBytesPerVector('int4', D)
                quantLabel = 'int4 quantized vectors'
                break
            case 'bbq':
                quantDisk = V * quantizedBytesPerVector('bbq', D)
                quantLabel = 'BBQ quantized vectors'
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
    } else if (indexType === 'disk_bbq') {
        const nc = Math.ceil(V / vpc)
        // Centroids: 7-bit OSQ quantized (1 byte/dim) + 16-byte correction.
        bbqCentroids = nc * (D + 16)
        // Cluster vectors: 1-bit codes at D≥384, else 4-bit (⌈D/2⌉); ×2 SOAR
        // overspill upper bound; + 16-byte correction (3 floats + 1 int).
        const clusterCodeBytes =
            D < DISKBBQ_ONE_BIT_MIN_DIMS ? Math.ceil(D / 2) : Math.ceil(D / 8)
        bbqVectors = V * 2 * (clusterCodeBytes + 16)
        indexDisk = bbqCentroids + bbqVectors
        indexLabel = 'DiskBBQ structures'
    }

    const totalDisk = rawDisk + quantDisk + indexDisk

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

    // --- RAM (off-heap working set) ---
    let ramVectors = 0
    let ramVectorsLabel = ''
    let ramIndex = 0
    let ramIndexLabel = ''

    if (indexType === 'hnsw' || indexType === 'flat') {
        switch (quantization) {
            case 'none':
                ramVectors = rawDisk
                ramVectorsLabel = 'Raw vectors in RAM'
                break
            case 'int8':
                ramVectors = V * quantizedBytesPerVector('int8', D)
                ramVectorsLabel = 'int8 vectors in RAM'
                break
            case 'int4':
                ramVectors = V * quantizedBytesPerVector('int4', D)
                ramVectorsLabel = 'int4 vectors in RAM'
                break
            case 'bbq':
                ramVectors = V * quantizedBytesPerVector('bbq', D)
                ramVectorsLabel = 'BBQ vectors in RAM'
                break
        }
        if (indexType === 'hnsw') {
            ramIndex = V * 4 * m
            ramIndexLabel = 'HNSW graph in RAM'
        }
    } else if (indexType === 'disk_bbq') {
        // All centroids stay resident; the selected % of posting lists
        // (cluster vectors) is cached in off-heap RAM.
        const pct = clampDiskBbqOffHeapPercent(offHeapRamPercent) / 100
        ramVectors = bbqCentroids + Math.ceil(bbqVectors * pct)
        ramVectorsLabel = 'DiskBBQ off-heap RAM'
    }

    const totalRam = ramVectors + ramIndex

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

    const totalCopies = 1 + replicas
    const clusterDisk = totalDisk * totalCopies
    const clusterRam = totalRam * totalCopies

    const components = buildComponents(inputs)
    const indexOptionsType = deriveIndexOptionsType(indexType, quantization)
    const diskToRamRatio = totalRam > 0 ? totalDisk / totalRam : 0

    return {
        diskBreakdown,
        ramBreakdown,
        components,
        indexOptionsType,
        totalDisk,
        totalRam,
        clusterDisk,
        clusterRam,
        diskToRamRatio,
        totalCopies,
    }
}

/** Split a hero byte total into value + unit. */
export function formatHeroSizeParts(bytes: number): {
    value: string
    unit: string
} {
    const { value, unit } = formatBytes(bytes)
    return { value, unit }
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
