import { calculate } from './calculations'
import { ComponentBreakdownTable } from './components/ComponentBreakdownTable'
import { ExplainersPanel } from './components/ExplainersPanel'
import type { CalculatorInputs } from './types'
import { EuiProvider } from '@elastic/eui'
import { fireEvent, render } from '@testing-library/react'

const inputs: CalculatorInputs = {
    numVectors: 1_000_000,
    numDimensions: 768,
    elementType: 'float',
    indexType: 'hnsw',
    quantization: 'bbq',
    replicas: 1,
    hnswM: 16,
    efConstruction: 100,
    vectorsPerCluster: 384,
    offHeapRamPercent: 10,
}

it('smoke: ComponentBreakdownTable renders rows + total + legend', () => {
    const result = calculate(inputs)
    const { container, getByText } = render(
        <EuiProvider colorMode="light">
            <ComponentBreakdownTable result={result} inputsValid />
        </EuiProvider>
    )
    expect(getByText('Component breakdown (per replica)')).toBeInTheDocument()
    expect(container.textContent).toContain('.veb') // BBQ quantized row
    expect(container.textContent).toContain('.vex') // HNSW graph row
    expect(container.textContent).toContain('index_options.type: bbq_hnsw')
    expect(container.textContent).toContain('Total on disk')
})

it('smoke: ExplainersPanel shows open sections and expands collapsed ones on click', () => {
    const { container, getByText } = render(
        <EuiProvider colorMode="light">
            <ExplainersPanel />
        </EuiProvider>
    )
    // header + section titles are always present
    expect(getByText('How it is computed')).toBeInTheDocument()
    expect(getByText('How each size is calculated')).toBeInTheDocument()

    // an initially-open section renders its body
    expect(container.textContent).toContain('working set that must stay')

    // a collapsed section's body is hidden until its header is clicked
    expect(container.textContent).not.toContain('graph connections per node')
    fireEvent.click(getByText('How each size is calculated'))
    expect(container.textContent).toContain('graph connections per node')
})
