import { clampEven, clusterTotalLabel, initVectorSizing } from './vector-sizing'

const form = `
<section data-calc hidden>
  <input data-calc-vectors value="1,000,000">
  <input data-calc-dimensions value="768">
  <select data-calc-element><option value="float" selected>float32</option><option value="byte">int8</option></select>
  <select data-calc-index><option value="hnsw" selected>HNSW</option><option value="flat">Flat</option><option value="disk_bbq">DiskBBQ</option></select>
  <p data-calc-index-note></p>
  <div data-calc-for="disk_bbq" hidden></div>
  <div data-calc-for="hnsw"></div>
  <input type="range" data-calc-offheap value="10"><input type="number" data-calc-offheap-number value="10">
  <input type="range" data-calc-m value="16"><input type="number" data-calc-m-number value="16">
  <input data-calc-cluster value="384">
  <div data-calc-quantization><select data-calc-quantization-select></select></div>
  <input data-calc-replicas value="0">
  <div data-calc-warning hidden><p data-calc-warning-text></p><a data-calc-warning-link hidden></a></div>
  <div data-calc-results>
  <p data-calc-total-label></p>
  <span data-calc-disk-value></span><span data-calc-disk-unit></span>
  <span data-calc-ram-value></span><span data-calc-ram-unit></span>
  <dd data-calc-type></dd><dd data-calc-quantization-label></dd><dd data-calc-disk></dd><dd data-calc-ram></dd>
  <p data-calc-ratio hidden></p><div data-calc-disclaimer hidden></div>
  </div>
</section>`

const $ = <T extends Element>(s: string) => document.querySelector<T>(s)!
const change = (el: Element, value: string) => {
    ;(el as HTMLInputElement).value = value
    el.dispatchEvent(new Event('input', { bubbles: true }))
}

describe('vector sizing', () => {
    beforeEach(() => {
        document.body.innerHTML = form
        initVectorSizing()
    })

    it('reveals the calculator and shows results for the defaults', () => {
        expect($<HTMLElement>('[data-calc]').hidden).toBe(false)
        expect($('[data-calc-type]').textContent).toContain('hnsw')
        expect($('[data-calc-disk-value]').textContent).not.toBe('')
        expect($('[data-calc-total-label]').textContent).toBe(
            'Cluster total (primary only):'
        )
    })

    it('defaults hnsw to bbq quantization', () => {
        expect(
            $<HTMLSelectElement>('[data-calc-quantization-select]').value
        ).toBe('bbq')
    })

    it('shows the rows that belong to the chosen index structure', () => {
        change($('[data-calc-index]'), 'disk_bbq')

        expect($<HTMLElement>('[data-calc-for="disk_bbq"]').hidden).toBe(false)
        expect($<HTMLElement>('[data-calc-for="hnsw"]').hidden).toBe(true)
        expect(
            $<HTMLSelectElement>('[data-calc-quantization-select]').disabled
        ).toBe(true)
    })

    it('falls back to hnsw when the element type cannot use DiskBBQ', () => {
        change($('[data-calc-index]'), 'disk_bbq')
        change($('[data-calc-element]'), 'byte')

        expect($<HTMLSelectElement>('[data-calc-index]').value).toBe('hnsw')
        expect($('[data-calc-index-note]').textContent).toContain('DiskBBQ')
    })

    it('hides quantization when there is a single option', () => {
        change($('[data-calc-element]'), 'byte')

        expect($<HTMLElement>('[data-calc-quantization]').hidden).toBe(true)
    })

    it('accepts shorthand vector counts', () => {
        change($('[data-calc-vectors]'), '10m')

        expect($('[data-calc-warning]').hasAttribute('hidden')).toBe(true)
        expect($('[data-calc-disk-value]').textContent).not.toBe('0')
    })

    it('warns and clears results above the dimension limit', () => {
        change($('[data-calc-dimensions]'), '5000')

        expect($<HTMLElement>('[data-calc-warning]').hidden).toBe(false)
        expect($<HTMLElement>('[data-calc-disclaimer]').hidden).toBe(true)
        expect($<HTMLElement>('[data-calc-results]').hidden).toBe(true)
    })

    it('clears results without a warning while a field is empty', () => {
        change($('[data-calc-dimensions]'), '')

        expect($<HTMLElement>('[data-calc-warning]').hidden).toBe(true)
        expect($<HTMLElement>('[data-calc-disclaimer]').hidden).toBe(true)
        expect($<HTMLElement>('[data-calc-results]').hidden).toBe(true)
    })

    it('shows the results again once the inputs are valid', () => {
        change($('[data-calc-dimensions]'), '')
        change($('[data-calc-dimensions]'), '768')

        expect($<HTMLElement>('[data-calc-results]').hidden).toBe(false)
    })

    it('keeps a quantization the user picks when the select fires input before change', () => {
        const select = $<HTMLSelectElement>('[data-calc-quantization-select]')
        const other = Array.from(select.options).find(
            (option) => option.value !== select.value
        )!
        select.value = other.value
        select.dispatchEvent(new Event('input', { bubbles: true }))
        select.dispatchEvent(new Event('change', { bubbles: true }))

        expect(select.value).toBe(other.value)
    })

    it('keeps the slider and number in step and clamps to an even value', () => {
        change($('[data-calc-m]'), '33')
        const number = $<HTMLInputElement>('[data-calc-m-number]')
        number.dispatchEvent(new Event('change'))

        expect(number.value).toBe('34')
    })

    it('labels the cluster total by replica count', () => {
        change($('[data-calc-replicas]'), '2')

        expect($('[data-calc-total-label]').textContent).toBe(
            'Cluster total (1 primary + 2 replicas):'
        )
    })

    it('binds each calculator once', () => {
        initVectorSizing()

        expect(document.querySelectorAll('[data-calc-bound]')).toHaveLength(1)
    })
})

describe('helpers', () => {
    it('rounds to an even value inside the range', () => {
        expect(clampEven(3, 2, 512)).toBe(4)
        expect(clampEven(1000, 2, 512)).toBe(512)
        expect(clampEven(0, 2, 512)).toBe(2)
    })

    it('words the replica totals', () => {
        expect(clusterTotalLabel(1)).toBe(
            'Cluster total (1 primary + 1 replica):'
        )
    })
})
