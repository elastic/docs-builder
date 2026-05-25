import { EuiIcon } from '@elastic/eui'
import { useState, type KeyboardEvent } from 'react'
import type { SizingFormulas } from '../types'

interface FormulasPanelProps {
    formulas: SizingFormulas
}

function isFormulaTotalLine(line: string): boolean {
    const trimmed = line.trim()
    if (!trimmed) return false
    return (
        trimmed.startsWith('Total disk (per replica)') ||
        trimmed.startsWith('Total off-heap RAM (per replica)') ||
        trimmed.startsWith('Cluster total disk') ||
        trimmed.startsWith('Cluster total RAM')
    )
}

function FormulaSection({ lines }: { lines: string[] }) {
    if (lines.length === 0) return null

    return (
        <pre className="vectorSizingCalc__formulasPre">
            {lines.map((line, index) => (
                <span
                    key={index}
                    className={
                        isFormulaTotalLine(line)
                            ? 'vectorSizingCalc__formulasLine vectorSizingCalc__formulasLine--total'
                            : 'vectorSizingCalc__formulasLine'
                    }
                >
                    {line}
                </span>
            ))}
        </pre>
    )
}

export function FormulasPanel({ formulas }: FormulasPanelProps) {
    const [isOpen, setIsOpen] = useState(false)

    const toggle = () => setIsOpen((open) => !open)

    const onHeaderKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault()
            toggle()
        }
    }

    const hasFormulas =
        formulas.disk.length > 0 ||
        formulas.ram.length > 0 ||
        formulas.cluster.length > 0

    return (
        <div className="vectorSizingCalc__formulasPanel">
            <div
                className="vectorSizingCalc__formulasHeader"
                role="button"
                tabIndex={0}
                aria-expanded={isOpen}
                onClick={toggle}
                onKeyDown={onHeaderKeyDown}
            >
                <span className="vectorSizingCalc__formulasTitle">
                    <EuiIcon type="flask" size="m" color="inherit" />
                    <span>Formulas</span>
                </span>
                <span className="vectorSizingCalc__formulasToggle">
                    {isOpen ? 'Hide formulas used' : 'Show formulas used'}
                    <EuiIcon
                        type={
                            isOpen
                                ? 'chevronSingleUp'
                                : 'chevronSingleDown'
                        }
                        size="m"
                    />
                </span>
            </div>
            {isOpen && hasFormulas && (
                <div className="vectorSizingCalc__formulasBody">
                    <div className="vectorSizingCalc__formulasSections">
                        <FormulaSection lines={formulas.disk} />
                        <FormulaSection lines={formulas.ram} />
                        <FormulaSection lines={formulas.cluster} />
                    </div>
                </div>
            )}
        </div>
    )
}
