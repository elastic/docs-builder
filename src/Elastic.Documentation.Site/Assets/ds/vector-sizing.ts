// Drives the vector sizing calculator: reads the native form controls, runs the shared calculation module and writes the
// results. The server renders the whole form hidden; this reveals it, because nothing here works without script.
import {
    calculate,
    clampDiskBbqOffHeapPercent,
    formatBytesString,
    formatHeroSizeParts,
    getAvailableQuantizations,
    validate,
} from '../web-components/VectorSizingCalculator/calculations'
import {
    formatDiskToRamSentence,
    formatGroupedInteger,
} from '../web-components/VectorSizingCalculator/formatNumbers'
import { parseVectorCount } from '../web-components/VectorSizingCalculator/parseVectorCount'
import type {
    CalculatorInputs,
    ElementType,
    IndexType,
    Quantization,
} from '../web-components/VectorSizingCalculator/types'

const DEFAULT_QUANTIZATION: Quantization = 'bbq'
const EF_CONSTRUCTION = 100
const DISK_BBQ_NOTE = 'DiskBBQ is available for float32 and bfloat16 vectors.'

export function clusterTotalLabel(replicas: number): string {
    if (replicas <= 0) return 'Cluster total (primary only):'
    if (replicas === 1) return 'Cluster total (1 primary + 1 replica):'
    return `Cluster total (1 primary + ${formatGroupedInteger(replicas)} replicas):`
}

export function clampEven(value: number, min: number, max: number): number {
    const clamped = Math.min(max, Math.max(min, Math.round(value)))
    return clamped % 2 === 0 ? clamped : Math.min(max, clamped + 1)
}

function clampInt(value: number, min: number, max: number, fallback: number) {
    if (!Number.isFinite(value)) return fallback
    return Math.min(max, Math.max(min, Math.round(value)))
}

export function initVectorSizing(scope: ParentNode = document) {
    scope
        .querySelectorAll<HTMLElement>('[data-calc]:not([data-calc-bound])')
        .forEach((root) => {
            root.dataset.calcBound = ''
            bindCalculator(root)
            root.hidden = false
        })
}

function bindCalculator(root: HTMLElement) {
    const q = <T extends HTMLElement>(selector: string) =>
        root.querySelector<T>(selector)!
    const text = (selector: string, value: string) => {
        q(selector).textContent = value
    }

    const vectors = q<HTMLInputElement>('[data-calc-vectors]')
    const dimensions = q<HTMLInputElement>('[data-calc-dimensions]')
    const element = q<HTMLSelectElement>('[data-calc-element]')
    const index = q<HTMLSelectElement>('[data-calc-index]')
    const quantization = q<HTMLSelectElement>('[data-calc-quantization-select]')
    const quantizationRow = q('[data-calc-quantization]')
    const replicas = q<HTMLInputElement>('[data-calc-replicas]')
    const cluster = q<HTMLInputElement>('[data-calc-cluster]')
    const mSlider = q<HTMLInputElement>('[data-calc-m]')
    const mNumber = q<HTMLInputElement>('[data-calc-m-number]')
    const offHeapSlider = q<HTMLInputElement>('[data-calc-offheap]')
    const offHeapNumber = q<HTMLInputElement>('[data-calc-offheap-number]')
    const indexNote = q('[data-calc-index-note]')

    let selectedQuantization: Quantization = DEFAULT_QUANTIZATION

    // The native number and range pairs share one value.
    const link = (
        a: HTMLInputElement,
        b: HTMLInputElement,
        normalize: (value: number) => number
    ) => {
        const sync = (from: HTMLInputElement, to: HTMLInputElement) => () => {
            const value = Number(from.value)
            if (from.value === '' || !Number.isFinite(value)) return
            to.value = String(normalize(value))
            if (from === b) a.value = to.value
            render()
        }
        a.addEventListener('input', sync(a, b))
        b.addEventListener('input', sync(b, a))
        b.addEventListener('change', () => {
            b.value = String(normalize(Number(b.value)))
            a.value = b.value
            render()
        })
    }
    link(mSlider, mNumber, (v) => clampEven(v, 2, 512))
    link(offHeapSlider, offHeapNumber, clampDiskBbqOffHeapPercent)

    const supportsDiskBbq = () =>
        element.value === 'float' || element.value === 'bfloat16'

    function syncIndexOptions() {
        const diskBbq = index.querySelector<HTMLOptionElement>(
            'option[value="disk_bbq"]'
        )!
        diskBbq.disabled = !supportsDiskBbq()
        if (diskBbq.disabled && index.value === 'disk_bbq') index.value = 'hnsw'
        indexNote.textContent = supportsDiskBbq() ? '' : DISK_BBQ_NOTE
    }

    function syncQuantizationOptions() {
        const options = getAvailableQuantizations(element.value, index.value)
        quantization.replaceChildren(
            ...options.map((option) => {
                const el = document.createElement('option')
                el.value = option.value
                el.textContent = option.label
                return el
            })
        )
        const values = options.map((o) => o.value)
        quantization.value = values.includes(selectedQuantization)
            ? selectedQuantization
            : values[0]
        quantization.disabled = index.value === 'disk_bbq'
        // One option is not a choice.
        quantizationRow.hidden = options.length <= 1
    }

    function syncRows() {
        root.querySelectorAll<HTMLElement>('[data-calc-for]').forEach((row) => {
            row.hidden = row.dataset.calcFor !== index.value
        })
    }

    function readInputs(): CalculatorInputs {
        return {
            numVectors: parseVectorCount(vectors.value),
            numDimensions: Number(dimensions.value.replace(/,/g, '')),
            elementType: element.value as ElementType,
            indexType: index.value as IndexType,
            quantization: (quantization.value || 'none') as Quantization,
            replicas: clampInt(Number(replicas.value), 0, 99, 0),
            hnswM: clampEven(Number(mNumber.value), 2, 512),
            efConstruction: EF_CONSTRUCTION,
            vectorsPerCluster: clampInt(
                Number(cluster.value),
                1,
                1_000_000,
                384
            ),
            offHeapRamPercent: clampDiskBbqOffHeapPercent(
                Number(offHeapNumber.value)
            ),
        }
    }

    function showWarning(message?: string, href?: string) {
        const warning = q('[data-calc-warning]')
        const link = q<HTMLAnchorElement>('[data-calc-warning-link]')
        warning.hidden = !message
        text('[data-calc-warning-text]', message ?? '')
        link.hidden = !(message && href)
        if (href) link.href = href
    }

    function render() {
        syncIndexOptions()
        syncQuantizationOptions()
        syncRows()

        const inputs = readInputs()
        const validation = validate(inputs)
        showWarning(validation.warning, validation.warningLink)
        const result = validation.valid ? calculate(inputs) : null
        // Without a valid calculation the previous figures no longer match the form, so they are hidden rather than left on show.
        q('[data-calc-results]').hidden = !result
        q('[data-calc-disclaimer]').hidden = !result
        if (!result) {
            q('[data-calc-ratio]').hidden = true
            return
        }

        const disk = formatHeroSizeParts(result.clusterDisk)
        const ram = formatHeroSizeParts(result.clusterRam)
        text('[data-calc-total-label]', clusterTotalLabel(inputs.replicas))
        text('[data-calc-disk-value]', disk.value)
        text('[data-calc-disk-unit]', `${disk.unit} Disk`)
        text('[data-calc-ram-value]', ram.value)
        text('[data-calc-ram-unit]', `${ram.unit} RAM`)
        text('[data-calc-type]', result.indexOptionsType)
        text(
            '[data-calc-quantization-label]',
            quantization.selectedOptions[0]?.textContent ?? 'None'
        )
        text('[data-calc-disk]', formatBytesString(result.totalDisk))
        text('[data-calc-ram]', formatBytesString(result.totalRam))
        const ratio = formatDiskToRamSentence(result.diskToRamRatio)
        text('[data-calc-ratio]', ratio)
        q('[data-calc-ratio]').hidden = ratio === ''
    }

    // A native select fires input before change, and render() rebuilds the options from selectedQuantization, so the choice has to be
    // remembered before any render listener runs, on both events.
    const rememberQuantization = () => {
        selectedQuantization = quantization.value as Quantization
    }
    quantization.addEventListener('input', rememberQuantization)
    quantization.addEventListener('change', rememberQuantization)
    for (const control of [
        vectors,
        dimensions,
        element,
        index,
        quantization,
        replicas,
        cluster,
    ]) {
        control.addEventListener('input', render)
        control.addEventListener('change', render)
    }

    render()
}
