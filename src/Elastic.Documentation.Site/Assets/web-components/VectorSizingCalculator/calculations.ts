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
export const DISKBBQ_OFF_HEAP_RAM_MIN_PERCENT = 5
export const DISKBBQ_OFF_HEAP_RAM_MAX_PERCENT = 10

/** DiskBBQ RAM range endpoints for hero/detail (fraction of posting lists cached; centroids always fully resident). */
const DISKBBQ_VECTOR_CACHE_MIN_FRACTION = 0.05
const DISKBBQ_VECTOR_CACHE_MAX_FRACTION = 0.1

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

    // Raw vectors (.vec) — always written, kept for rescoring.
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
        source: 'LuceneFilesExtensions.java:81 · float=4 / byte=1 / bit=⌈D/8⌉ · bfloat16=2 BFloat16.java:20',
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
            source: 'DenseVectorFieldMapper.java:2095 · +16 = 3×float+int Int7uOSQVectorScorerSupplier.java:42,57',
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
            source: 'DenseVectorFieldMapper.java:2290 · ⌈D/2⌉+16 Int4VectorScorerSupplier.java:49',
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
            source: 'ES818BinaryQuantizedVectorsWriter · +14 ES93BinaryQuantizedVectorScorer.java:23 · discretize BQVectorUtils.java:44',
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
            source: 'LuceneFilesExtensions.java:82 · m = DEFAULT_MAX_CONN = 16 DenseVectorFieldMapper.java:138',
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
            source: 'ES940DiskBBQVectorsFormat.java:55 · C = 384 (:80) · writeQuantizedValue ES940DiskBBQVectorsWriter.java:860-864',
        })
        const oneBit = D >= DISKBBQ_ONE_BIT_MIN_DIMS
        const codeBytes = oneBit ? Math.ceil(D / 8) : Math.ceil(D / 2)
        rows.push({
            name: 'DiskBBQ clusters (quantized vectors)',
            ext: 'clivf',
            formula: oneBit ? 'V × 2 × (⌈D/8⌉ + 16)' : 'V × 2 × (⌈D/2⌉ + 16)',
            detail: `${int(V)} × 2 × (${int(codeBytes)} + 16)`,
            bytes: V * 2 * (codeBytes + 16),
            offHeap: 'partial',
            description: `${oneBit ? '1' : '4'}-bit OSQ vectors + 16-byte correction; D≥384 uses 1-bit, else 4-bit. The ×2 is a conservative SOAR-overspill upper bound. Only touched clusters are paged in.`,
            source: 'ES940DiskBBQVectorsFormat.java:57 · bits DenseVectorFieldMapper.java:511 · SOAR ES940DiskBBQVectorsWriter.java:272-276',
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
                quantDisk = V * quantizedBytesPerVector('int8', D)
                quantLabel = 'int8 quantized vectors'
                diskFormulas.push(
                    `int8 quantized = V × (D + 16) = ${formatBytesString(quantDisk)}`
                )
                break
            case 'int4':
                quantDisk = V * quantizedBytesPerVector('int4', D)
                quantLabel = 'int4 quantized vectors'
                diskFormulas.push(
                    `int4 quantized = V × (⌈D/2⌉ + 16) = ${formatBytesString(quantDisk)}`
                )
                break
            case 'bbq':
                quantDisk = V * quantizedBytesPerVector('bbq', D)
                quantLabel = 'BBQ quantized vectors'
                diskFormulas.push(
                    `BBQ quantized = V × (⌈D/64⌉×8 + 14) = ${formatBytesString(quantDisk)}`
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
        // Centroids: 7-bit OSQ quantized (1 byte/dim) + 16-byte correction.
        bbqCentroids = nc * (D + 16)
        // Cluster vectors: 1-bit codes at D≥384, else 4-bit (⌈D/2⌉); ×2 SOAR
        // overspill upper bound; + 16-byte correction (3 floats + 1 int).
        const clusterCodeBytes =
            D < DISKBBQ_ONE_BIT_MIN_DIMS ? Math.ceil(D / 2) : Math.ceil(D / 8)
        const clusterCodeLabel =
            D < DISKBBQ_ONE_BIT_MIN_DIMS ? '⌈D/2⌉' : '⌈D/8⌉'
        bbqVectors = V * 2 * (clusterCodeBytes + 16)
        indexDisk = bbqCentroids + bbqVectors
        indexLabel = 'DiskBBQ structures'
        diskFormulas.push(
            `DiskBBQ clusters = ⌈V / ${vpc}⌉ = ${formatGroupedInteger(nc)}`
        )
        diskFormulas.push(
            `Centroid bytes = ⌈V/${vpc}⌉ × (D + 16) = ${formatBytesString(bbqCentroids)}`
        )
        diskFormulas.push(
            `Quantized cluster vectors = V × 2 × (${clusterCodeLabel} + 16) = ${formatBytesString(bbqVectors)}`
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
    let ramVectorsLabel = ''
    let ramIndex = 0
    let ramIndexLabel = ''
    let diskBbqRamMin = 0
    let diskBbqRamMax = 0

    if (indexType === 'hnsw' || indexType === 'flat') {
        switch (quantization) {
            case 'none':
                ramVectors = rawDisk
                ramVectorsLabel = 'Raw vectors in RAM'
                ramFormulas.push(
                    `Vector RAM = ${formatBytesString(ramVectors)}`
                )
                break
            case 'int8':
                ramVectors = V * quantizedBytesPerVector('int8', D)
                ramVectorsLabel = 'int8 vectors in RAM'
                ramFormulas.push(
                    `Vector RAM (int8) = ${formatBytesString(ramVectors)}`
                )
                break
            case 'int4':
                ramVectors = V * quantizedBytesPerVector('int4', D)
                ramVectorsLabel = 'int4 vectors in RAM'
                ramFormulas.push(
                    `Vector RAM (int4) = ${formatBytesString(ramVectors)}`
                )
                break
            case 'bbq':
                ramVectors = V * quantizedBytesPerVector('bbq', D)
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
        diskBbqRamMin =
            bbqCentroids +
            Math.ceil(bbqVectors * DISKBBQ_VECTOR_CACHE_MIN_FRACTION)
        diskBbqRamMax =
            bbqCentroids +
            Math.ceil(bbqVectors * DISKBBQ_VECTOR_CACHE_MAX_FRACTION)
        const ramSelected = bbqCentroids + Math.ceil(bbqVectors * pct)
        ramVectors = ramSelected
        ramVectorsLabel = 'DiskBBQ off-heap RAM'
        const minPct = DISKBBQ_VECTOR_CACHE_MIN_FRACTION * 100
        const maxPct = DISKBBQ_VECTOR_CACHE_MAX_FRACTION * 100
        ramFormulas.push(
            `DiskBBQ RAM min = centroids + ${minPct}% × posting lists = ${formatBytesString(diskBbqRamMin)}`
        )
        ramFormulas.push(
            `DiskBBQ RAM max = centroids + ${maxPct}% × posting lists = ${formatBytesString(diskBbqRamMax)}`
        )
        ramFormulas.push(
            `DiskBBQ RAM (${clampDiskBbqOffHeapPercent(offHeapRamPercent)}% posting lists cached) = centroids + ${clampDiskBbqOffHeapPercent(offHeapRamPercent)}% × posting lists = ${formatBytesString(ramSelected)}`
        )
        ramFormulas.push(
            '  All centroids stay resident; the cached posting-list fraction depends on throughput/latency goals'
        )
    }

    const usesRamRange = indexType === 'disk_bbq'
    const totalRam = ramVectors + ramIndex
    const totalRamMin =
        indexType === 'disk_bbq' ? diskBbqRamMin + ramIndex : totalRam
    const totalRamMax =
        indexType === 'disk_bbq' ? diskBbqRamMax + ramIndex : totalRam
    ramFormulas.push(
        usesRamRange
            ? `Total off-heap RAM (per replica) = ${formatBytesRangeLabel(totalRamMin, totalRamMax)}`
            : `Total off-heap RAM (per replica) = ${formatBytesString(totalRam)}`
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

    const totalCopies = 1 + replicas
    const clusterDisk = totalDisk * totalCopies
    const clusterRam = totalRam * totalCopies
    const clusterRamMax = totalRamMax * totalCopies
    const clusterRamMin = totalRamMin * totalCopies

    clusterFormulas.push(
        `Cluster total disk = per-replica × ${totalCopies} copies = ${formatBytesString(clusterDisk)}`
    )
    clusterFormulas.push(
        usesRamRange
            ? `Cluster total RAM  = per-replica × ${totalCopies} copies = ${formatBytesRangeLabel(clusterRamMin, clusterRamMax)}`
            : `Cluster total RAM  = per-replica × ${totalCopies} copies = ${formatBytesString(clusterRam)}`
    )

    const components = buildComponents(inputs)
    const indexOptionsType = deriveIndexOptionsType(indexType, quantization)
    const diskToRamRatioMin = totalRamMax > 0 ? totalDisk / totalRamMax : 0
    const diskToRamRatioMax = totalRamMin > 0 ? totalDisk / totalRamMin : 0

    return {
        diskBreakdown,
        ramBreakdown,
        components,
        indexOptionsType,
        totalDisk,
        totalRam,
        totalRamMin,
        totalRamMax,
        clusterDisk,
        clusterRam,
        clusterRamMin,
        clusterRamMax,
        usesRamRange,
        diskToRamRatioMin,
        diskToRamRatioMax,
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
    if (minBytes <= 0 && maxBytes <= 0) return '0 MiB'
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

/** Split hero label into value + unit for DiskBBQ RAM ranges. */
export function formatHeroSizeParts(
    bytes: number,
    bytesMin?: number,
    bytesMax?: number
): { value: string; unit: string } {
    if (
        bytesMin !== undefined &&
        bytesMax !== undefined &&
        bytesMin !== bytesMax
    ) {
        const label = formatRamHeroLabel(bytesMin, bytesMax)
        const lastSpace = label.lastIndexOf(' ')
        if (lastSpace > 0) {
            return {
                value: label.slice(0, lastSpace),
                unit: label.slice(lastSpace + 1),
            }
        }
        return { value: label, unit: '' }
    }

    const { value, unit } = formatBytes(bytes)
    return { value, unit }
}

function formatRamHeroSingle(bytes: number): string {
    if (bytes <= 0) return '0 MiB'
    const gb = bytes / 1024 ** 3
    if (gb >= 1) {
        return gb >= 10 ? `${Math.round(gb)} GiB` : `${gb.toFixed(1)} GiB`
    }
    const mb = bytes / 1024 ** 2
    return mb >= 10 ? `${Math.round(mb)} MiB` : `${mb.toFixed(1)} MiB`
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
