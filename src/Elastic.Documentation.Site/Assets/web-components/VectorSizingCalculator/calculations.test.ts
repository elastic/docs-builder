import { calculate, getAvailableQuantizations, validate } from './calculations'
import type { BreakdownItem, CalculatorInputs } from './types'

const V = 1_000_000

const base: CalculatorInputs = {
    numVectors: V,
    numDimensions: 768,
    elementType: 'float',
    indexType: 'hnsw',
    quantization: 'none',
    replicas: 0,
    hnswM: 16,
    efConstruction: 200,
    vectorsPerCluster: 384,
    offHeapRamPercent: 5,
}

function make(overrides: Partial<CalculatorInputs>): CalculatorInputs {
    return { ...base, ...overrides }
}

function bytesFor(items: BreakdownItem[], label: string): number {
    const item = items.find((i) => i.label === label)
    return item ? item.bytes : 0
}

describe('rawBytesPerVector (via flat + no quantization)', () => {
    // flat index with quantization 'none' => totalDisk is exactly the raw vectors.
    it.each([
        ['float', 768 * 4],
        ['bfloat16', 768 * 2],
        ['byte', 768],
        ['bit', 96], // ceil(768 / 8)
    ] as const)('%s raw = V x %i bytes/vector', (elementType, perVector) => {
        const r = calculate(
            make({ elementType, indexType: 'flat', quantization: 'none' })
        )!
        expect(r.totalDisk).toBe(V * perVector)
    })
})

describe('int8 / int4 corrections are 16 bytes (current OSQ codec), not the legacy 4', () => {
    it('int8 quantized = V x (D + 16)', () => {
        const r = calculate(make({ quantization: 'int8', numDimensions: 768 }))!
        // regression guard: +4 would give 772_000_000
        expect(bytesFor(r.diskBreakdown, 'int8 quantized vectors')).toBe(
            V * (768 + 16)
        )
        expect(bytesFor(r.ramBreakdown, 'int8 vectors in RAM')).toBe(
            V * (768 + 16)
        )
    })

    it('int8 correction is visible at low dims (D=100 -> 116 B/vec)', () => {
        const r = calculate(make({ quantization: 'int8', numDimensions: 100 }))!
        expect(bytesFor(r.diskBreakdown, 'int8 quantized vectors')).toBe(
            V * (100 + 16)
        )
    })

    it('int4 quantized = V x (ceil(D/2) + 16)', () => {
        const r = calculate(make({ quantization: 'int4', numDimensions: 768 }))!
        expect(bytesFor(r.diskBreakdown, 'int4 quantized vectors')).toBe(
            V * (384 + 16)
        )
        expect(bytesFor(r.ramBreakdown, 'int4 vectors in RAM')).toBe(
            V * (384 + 16)
        )
    })

    it('int4 packs odd dims with ceil (D=101 -> 51 + 16)', () => {
        const r = calculate(make({ quantization: 'int4', numDimensions: 101 }))!
        expect(bytesFor(r.diskBreakdown, 'int4 quantized vectors')).toBe(
            V * (51 + 16)
        )
    })
})

describe('BBQ bit payload is discretized to a multiple of 64 (+14 short correction)', () => {
    it('D multiple of 64: D=768 -> 96 + 14', () => {
        const r = calculate(make({ quantization: 'bbq', numDimensions: 768 }))!
        expect(bytesFor(r.diskBreakdown, 'BBQ quantized vectors')).toBe(
            V * (96 + 14)
        )
    })

    it('D not a multiple of 64: D=100 -> ceil(100/64)*8 (=16) + 14', () => {
        // regression guard: ceil(100/8)=13 would give 27 B/vec, not 30.
        const r = calculate(make({ quantization: 'bbq', numDimensions: 100 }))!
        expect(bytesFor(r.diskBreakdown, 'BBQ quantized vectors')).toBe(
            V * (16 + 14)
        )
        expect(bytesFor(r.ramBreakdown, 'BBQ vectors in RAM')).toBe(
            V * (16 + 14)
        )
    })

    it('D just over a 64 boundary: D=65 -> 16 + 14', () => {
        const r = calculate(make({ quantization: 'bbq', numDimensions: 65 }))!
        expect(bytesFor(r.diskBreakdown, 'BBQ quantized vectors')).toBe(
            V * (16 + 14)
        )
    })
})

describe('HNSW graph = V x 4 x m', () => {
    it('m=16 -> 64 bytes/vector', () => {
        const r = calculate(make({ quantization: 'int8', hnswM: 16 }))!
        expect(bytesFor(r.diskBreakdown, 'HNSW graph')).toBe(V * 4 * 16)
        expect(bytesFor(r.ramBreakdown, 'HNSW graph in RAM')).toBe(V * 4 * 16)
    })

    it('flat index has no graph', () => {
        const r = calculate(make({ quantization: 'int8', indexType: 'flat' }))!
        expect(bytesFor(r.diskBreakdown, 'HNSW graph')).toBe(0)
    })
})

describe('DiskBBQ centroids and clusters', () => {
    // nc = ceil(1_000_000 / 384) = 2605
    const nc = Math.ceil(V / 384)

    it('centroids are 7-bit OSQ quantized: nc x (D + 16), not float32', () => {
        const D = 768
        const r = calculate(
            make({
                indexType: 'disk_bbq',
                quantization: 'bbq',
                numDimensions: D,
            })
        )!
        const clusters = V * 2 * (Math.ceil(D / 8) + 16) // 1-bit at D>=384
        const centroids = nc * (D + 16)
        const indexDisk = bytesFor(r.diskBreakdown, 'DiskBBQ structures')
        expect(indexDisk).toBe(centroids + clusters)
        // isolate the centroid term (regression: nc*D*4 + nc*(D+14) ~= 5x bigger)
        expect(indexDisk - clusters).toBe(centroids)
    })

    it('clusters use 1-bit codes at D >= 384: V x 2 x (ceil(D/8) + 16)', () => {
        const D = 768
        const r = calculate(
            make({
                indexType: 'disk_bbq',
                quantization: 'bbq',
                numDimensions: D,
            })
        )!
        const centroids = nc * (D + 16)
        const indexDisk = bytesFor(r.diskBreakdown, 'DiskBBQ structures')
        expect(indexDisk - centroids).toBe(V * 2 * (96 + 16))
    })

    it('clusters use 4-bit codes below 384 dims: V x 2 x (ceil(D/2) + 16)', () => {
        const D = 256
        const r = calculate(
            make({
                indexType: 'disk_bbq',
                quantization: 'bbq',
                numDimensions: D,
            })
        )!
        const centroids = nc * (D + 16)
        const indexDisk = bytesFor(r.diskBreakdown, 'DiskBBQ structures')
        // regression guard: 1-bit would be V*2*(32+16)=96M, 4-bit is V*2*(128+16)=288M
        expect(indexDisk - centroids).toBe(V * 2 * (128 + 16))
    })

    it('raw float vectors are still retained on disk for disk_bbq', () => {
        const D = 768
        const r = calculate(
            make({
                indexType: 'disk_bbq',
                quantization: 'bbq',
                numDimensions: D,
            })
        )!
        expect(bytesFor(r.diskBreakdown, 'Raw vectors')).toBe(V * D * 4)
    })

    it('off-heap RAM range = all centroids + (5%..10%) of posting lists', () => {
        const D = 768
        const r = calculate(
            make({
                indexType: 'disk_bbq',
                quantization: 'bbq',
                numDimensions: D,
            })
        )!
        const centroids = nc * (D + 16)
        const clusters = V * 2 * (96 + 16)
        expect(r.usesRamRange).toBe(true)
        expect(r.totalRamMin).toBe(centroids + Math.ceil(clusters * 0.05))
        expect(r.totalRamMax).toBe(centroids + Math.ceil(clusters * 0.1))
    })
})

describe('cluster totals scale with copies (1 primary + replicas)', () => {
    it('replicas=1 doubles per-replica disk and RAM', () => {
        const r = calculate(make({ quantization: 'int8', replicas: 1 }))!
        expect(r.totalCopies).toBe(2)
        expect(r.clusterDisk).toBe(r.totalDisk * 2)
        expect(r.clusterRam).toBe(r.totalRam * 2)
    })
})

describe('validation and quantization availability', () => {
    it('rejects more than 4096 dimensions', () => {
        expect(validate(make({ numDimensions: 5000 })).valid).toBe(false)
    })

    it('warns that quantization does not apply to byte vectors', () => {
        const res = validate(
            make({ elementType: 'byte', quantization: 'int8' })
        )
        expect(res.warning).toContain('not applicable')
    })

    it('disk_bbq only offers the built-in BBQ quantization', () => {
        const opts = getAvailableQuantizations('float', 'disk_bbq')
        expect(opts.map((o) => o.value)).toEqual(['bbq'])
    })

    it('float hnsw offers none/int8/int4/bbq', () => {
        const opts = getAvailableQuantizations('float', 'hnsw')
        expect(opts.map((o) => o.value)).toEqual([
            'none',
            'int8',
            'int4',
            'bbq',
        ])
    })
})
