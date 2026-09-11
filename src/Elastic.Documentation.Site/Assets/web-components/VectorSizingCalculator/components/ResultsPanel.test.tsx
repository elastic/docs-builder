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
    it('labels RAM plainly and states disk relative to RAM needed for search', () => {
        renderPanel()

        expect(screen.getByText('RAM per replica:')).toBeInTheDocument()
        expect(
            screen.getByText(/Disk is about .+ the RAM needed for search\./)
        ).toBeInTheDocument()
        expect(screen.queryByText(/Off-heap RAM/)).not.toBeInTheDocument()
        expect(
            screen.queryByText(/Disk : off-heap RAM/)
        ).not.toBeInTheDocument()
    })

    it('calls out serverless on its own line and does not link away', () => {
        renderPanel()

        expect(
            screen.getByText('Does not apply to Elastic Cloud Serverless.')
        ).toBeInTheDocument()
        expect(
            screen.getByText(
                /self-managed Elasticsearch and Elastic Cloud Hosted/
            )
        ).toBeInTheDocument()
        expect(screen.queryByText('Learn more')).not.toBeInTheDocument()
    })

    it('hides the compactness sentence and disclaimer when inputs are invalid', () => {
        renderPanel({ result: null, inputsValid: false })

        expect(
            screen.queryByText(/RAM needed for search/)
        ).not.toBeInTheDocument()
        expect(
            screen.queryByText(/Elastic Cloud Serverless/)
        ).not.toBeInTheDocument()
        expect(screen.getByText('RAM per replica:')).toBeInTheDocument()
    })
})
