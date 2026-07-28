import { formatBytesString } from '../calculations'
import type { OffHeapResidency, SizingResult } from '../types'
import {
    EuiBadge,
    EuiCode,
    EuiIcon,
    EuiTable,
    EuiTableBody,
    EuiTableHeader,
    EuiTableHeaderCell,
    EuiTableRow,
    EuiTableRowCell,
} from '@elastic/eui'

interface ComponentBreakdownTableProps {
    result: SizingResult | null
    inputsValid: boolean
}

const OFF_HEAP_BADGE: Record<
    OffHeapResidency,
    { label: string; color: 'success' | 'warning' | 'hollow' }
> = {
    yes: { label: 'Yes', color: 'success' },
    partial: { label: 'Partial', color: 'warning' },
    no: { label: 'No', color: 'hollow' },
}

export function ComponentBreakdownTable({
    result,
    inputsValid,
}: ComponentBreakdownTableProps) {
    if (!inputsValid || result === null) {
        return null
    }

    const { components, totalDisk, indexOptionsType } = result

    return (
        <div className="vectorSizingCalc__breakdownPanel">
            <div className="vectorSizingCalc__breakdownHeader">
                <span className="vectorSizingCalc__breakdownTitle">
                    <EuiIcon type="database" size="m" color="inherit" />
                    <span>Component breakdown (per replica)</span>
                </span>
                <EuiBadge color="hollow">
                    index_options.type: {indexOptionsType}
                </EuiBadge>
            </div>

            <div className="vectorSizingCalc__breakdownScroll">
                <EuiTable
                    className="vectorSizingCalc__breakdownTable"
                    responsiveBreakpoint={false}
                    compressed
                >
                    <EuiTableHeader>
                        <EuiTableHeaderCell>Component</EuiTableHeaderCell>
                        <EuiTableHeaderCell>File</EuiTableHeaderCell>
                        <EuiTableHeaderCell>Formula</EuiTableHeaderCell>
                        <EuiTableHeaderCell align="right">
                            Size
                        </EuiTableHeaderCell>
                        <EuiTableHeaderCell>Off-heap RAM</EuiTableHeaderCell>
                        <EuiTableHeaderCell>What it is</EuiTableHeaderCell>
                    </EuiTableHeader>

                    <EuiTableBody>
                        {components.map((component) => {
                            const badge = OFF_HEAP_BADGE[component.offHeap]
                            return (
                                <EuiTableRow
                                    key={`${component.ext}-${component.name}`}
                                >
                                    <EuiTableRowCell className="vectorSizingCalc__breakdownName">
                                        {component.name}
                                    </EuiTableRowCell>
                                    <EuiTableRowCell>
                                        <EuiCode>.{component.ext}</EuiCode>
                                    </EuiTableRowCell>
                                    <EuiTableRowCell>
                                        <span className="vectorSizingCalc__formulaCell">
                                            <EuiCode>
                                                {component.formula}
                                            </EuiCode>
                                            <span className="vectorSizingCalc__formulaDetail">
                                                {component.detail}
                                            </span>
                                            <span className="vectorSizingCalc__formulaSource">
                                                {component.source}
                                            </span>
                                        </span>
                                    </EuiTableRowCell>
                                    <EuiTableRowCell
                                        align="right"
                                        className="vectorSizingCalc__breakdownNum"
                                    >
                                        {formatBytesString(component.bytes)}
                                    </EuiTableRowCell>
                                    <EuiTableRowCell>
                                        <EuiBadge color={badge.color}>
                                            {badge.label}
                                        </EuiBadge>
                                    </EuiTableRowCell>
                                    <EuiTableRowCell className="vectorSizingCalc__breakdownDesc">
                                        {component.description}
                                    </EuiTableRowCell>
                                </EuiTableRow>
                            )
                        })}

                        <EuiTableRow className="vectorSizingCalc__breakdownTotalRow">
                            <EuiTableRowCell className="vectorSizingCalc__breakdownName">
                                Total on disk
                            </EuiTableRowCell>
                            <EuiTableRowCell />
                            <EuiTableRowCell />
                            <EuiTableRowCell
                                align="right"
                                className="vectorSizingCalc__breakdownNum"
                            >
                                {formatBytesString(totalDisk)}
                            </EuiTableRowCell>
                            <EuiTableRowCell />
                            <EuiTableRowCell />
                        </EuiTableRow>
                    </EuiTableBody>
                </EuiTable>
            </div>

            <div className="vectorSizingCalc__breakdownLegend">
                <span>
                    <EuiBadge color="success">Yes</EuiBadge> must stay resident
                    (filesystem cache)
                </span>
                <span>
                    <EuiBadge color="warning">Partial</EuiBadge> only touched
                    parts paged in
                </span>
                <span>
                    <EuiBadge color="hollow">No</EuiBadge> lives on disk, read
                    on demand
                </span>
            </div>
        </div>
    )
}
