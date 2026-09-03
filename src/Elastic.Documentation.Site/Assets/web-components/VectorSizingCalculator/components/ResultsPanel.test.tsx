import { calculate } from '../calculations'
import type { CalculatorInputs, ValidationResult } from '../types'
import { ResultsPanel } from './ResultsPanel'
import { EuiProvider } from '@elastic/eui'
import { render, screen } from '@testing-library/react'
import type { ComponentProps } from 'react'

const inputs: CalculatorInputs = {
    numVectors: 1_000_000,
    numDimensions: 768,
    elementType: 'float',
    indexType: 'hnsw',
    quantization: 'bbq',
    replicas: 0,
    hnswM: 16,
    efConstruction: 100,
    vectorsPerCluster: 384,
    offHeapRamPercent: 10,
}

const valid: ValidationResult = { valid: true }

function renderPanel(
    overrides: Partial<ComponentProps<typeof ResultsPanel>> = {}
) {
    const result = calculate(inputs)
    return render(
        <EuiProvider
            colorMode="light"
            globalStyles={false}
            utilityClasses={false}
        >
            <ResultsPanel
                result={result}
                inputsValid
                quantizationLabel="BBQ"
                replicas={0}
                validation={valid}
                {...overrides}
            />
        </EuiProvider>
    )
}

describe('ResultsPanel', () => {
    it('labels RAM as off-heap and states disk relative to that working set', () => {
        renderPanel()

        expect(screen.getAllByText(/Off-heap RAM/).length).toBeGreaterThan(0)
        expect(
            screen.getByText('Off-heap RAM per replica:')
        ).toBeInTheDocument()
        expect(
            screen.getByText(/Disk is about .+ the off-heap RAM working set\./)
        ).toBeInTheDocument()
        expect(
            screen.queryByText(/Disk : off-heap RAM/)
        ).not.toBeInTheDocument()
    })

    it('scopes the estimate to self-managed and Elastic Cloud Hosted', () => {
        renderPanel()

        expect(
            screen.getByText(
                /self-managed Elasticsearch and Elastic Cloud Hosted/
            )
        ).toBeInTheDocument()
        expect(
            screen.getByText(/not how you size Elastic Cloud Serverless/)
        ).toBeInTheDocument()
        expect(screen.getByText(/not JVM heap/)).toBeInTheDocument()
    })

    it('hides the compactness sentence and disclaimer when inputs are invalid', () => {
        renderPanel({ result: null, inputsValid: false })

        expect(
            screen.queryByText(/off-heap RAM working set/)
        ).not.toBeInTheDocument()
        expect(
            screen.queryByText(/Elastic Cloud Serverless/)
        ).not.toBeInTheDocument()
        expect(
            screen.getByText('Off-heap RAM per replica:')
        ).toBeInTheDocument()
    })
})
