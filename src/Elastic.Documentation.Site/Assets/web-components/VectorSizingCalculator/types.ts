export type ElementType = 'float' | 'bfloat16' | 'byte' | 'bit'
export type IndexType = 'hnsw' | 'flat' | 'disk_bbq'
export type Quantization = 'none' | 'int8' | 'int4' | 'bbq'

export interface CalculatorInputs {
    numVectors: number
    numDimensions: number
    elementType: ElementType
    indexType: IndexType
    quantization: Quantization
    /** Replica shards only (excludes primary). */
    replicas: number
    hnswM: number
    efConstruction: number
    vectorsPerCluster: number
    /** DiskBBQ: share of quantized vectors cached in off-heap RAM (0–100). */
    offHeapRamPercent: number
}

export interface BreakdownItem {
    label: string
    bytes: number
    color: 'primary' | 'accent' | 'warning'
}

export interface SizingFormulas {
    disk: string[]
    ram: string[]
    cluster: string[]
}

export interface SizingResult {
    diskBreakdown: BreakdownItem[]
    ramBreakdown: BreakdownItem[]
    totalDisk: number
    /** Per-replica RAM at the selected DiskBBQ allocation (or exact value for other index types). */
    totalRam: number
    /** Per-replica RAM lower bound (DiskBBQ 0% vector cache). */
    totalRamMin: number
    /** Per-replica RAM upper bound (DiskBBQ 100% vector cache). */
    totalRamMax: number
    clusterDisk: number
    clusterRam: number
    clusterRamMin: number
    clusterRamMax: number
    /** DiskBBQ: hero and per-replica RAM show min–max (5%–50% vector cache). */
    usesRamRange: boolean
    /** Index copies = 1 primary + replicas. */
    totalCopies: number
    formulas: SizingFormulas
}

export interface ValidationResult {
    valid: boolean
    warning?: string
    warningLink?: string
    note?: string
}
