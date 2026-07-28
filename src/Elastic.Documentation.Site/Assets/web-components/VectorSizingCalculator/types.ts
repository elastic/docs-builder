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

/** Whether a component must stay in the OS filesystem cache (off-heap) for fast search. */
export type OffHeapResidency = 'yes' | 'partial' | 'no'

/**
 * One physical data structure written for a `dense_vector` field, mirroring the
 * per-file breakdown Elasticsearch reports under `off_heap.*_size_bytes`.
 */
export interface ComponentRow {
    name: string
    /** Lucene/ES file extension without the dot, e.g. `vec`, `veq`, `veb`, `vex`, `cenivf`, `clivf`. */
    ext: string
    /** Symbolic per-structure formula, e.g. `V × (D + 16)`. */
    formula: string
    /** Formula with the current input values substituted in. */
    detail: string
    bytes: number
    offHeap: OffHeapResidency
    description: string
    /** Verified Elasticsearch source reference(s) for the constants. */
    source: string
}

export interface SizingFormulas {
    disk: string[]
    ram: string[]
    cluster: string[]
}

export interface SizingResult {
    diskBreakdown: BreakdownItem[]
    ramBreakdown: BreakdownItem[]
    /** Per-structure breakdown (per replica) for the component table + file references. */
    components: ComponentRow[]
    /** Canonical `index_options.type` name, e.g. `bbq_hnsw`, `int8_flat`, `bbq_disk`. */
    indexOptionsType: string
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
    /** DiskBBQ: hero and per-replica RAM show min–max (5%–10% posting-list cache). */
    usesRamRange: boolean
    /** Disk ÷ off-heap RAM ratio (per replica). For DiskBBQ this is the min–max band. */
    diskToRamRatioMin: number
    diskToRamRatioMax: number
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
