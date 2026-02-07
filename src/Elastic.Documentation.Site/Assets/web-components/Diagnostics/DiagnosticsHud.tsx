import { useDiagnosticsStore, DiagnosticItem } from './diagnostics.store'
import * as React from 'react'
import { useEffect, useRef, useMemo, useCallback } from 'react'

// Close icon
const CloseIcon: React.FC = () => (
    <svg
        className="w-5 h-5"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
    >
        <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M6 18L18 6M6 6l12 12"
        />
    </svg>
)

// Copy icon
const CopyIcon: React.FC = () => (
    <svg
        className="w-4 h-4"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
    >
        <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
        />
    </svg>
)

// Check icon for copy confirmation
const CheckIcon: React.FC = () => (
    <svg
        className="w-4 h-4"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
    >
        <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M5 13l4 4L19 7"
        />
    </svg>
)

// Error icon
const ErrorIcon: React.FC = () => (
    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
        <path
            fillRule="evenodd"
            d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
            clipRule="evenodd"
        />
    </svg>
)

// Warning icon
const WarningIcon: React.FC = () => (
    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
        <path
            fillRule="evenodd"
            d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
            clipRule="evenodd"
        />
    </svg>
)

// Hint icon
const HintIcon: React.FC = () => (
    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
        <path
            fillRule="evenodd"
            d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
            clipRule="evenodd"
        />
    </svg>
)

const getSeverityStyles = (severity: DiagnosticItem['severity']) => {
    switch (severity) {
        case 'error':
            return {
                border: 'border-l-red',
                bg: 'bg-red/10',
                icon: 'text-red',
            }
        case 'warning':
            return {
                border: 'border-l-yellow',
                bg: 'bg-yellow/10',
                icon: 'text-yellow',
            }
        case 'hint':
            return {
                border: 'border-l-blue-elastic',
                bg: 'bg-blue-elastic/10',
                icon: 'text-blue-elastic',
            }
    }
}

const getSeverityIcon = (severity: DiagnosticItem['severity']) => {
    switch (severity) {
        case 'error':
            return <ErrorIcon />
        case 'warning':
            return <WarningIcon />
        case 'hint':
            return <HintIcon />
    }
}

const DiagnosticRow: React.FC<{ diagnostic: DiagnosticItem }> = ({
    diagnostic,
}) => {
    const styles = getSeverityStyles(diagnostic.severity)
    const [copied, setCopied] = React.useState(false)

    // Extract just the filename from the full path
    const fileName = diagnostic.file.split('/').pop() || diagnostic.file

    const handleCopy = useCallback(async () => {
        const location = diagnostic.line
            ? `${diagnostic.file}:${diagnostic.line}${diagnostic.column ? `:${diagnostic.column}` : ''}`
            : diagnostic.file
        const text = `[${diagnostic.severity.toUpperCase()}] ${location}: ${diagnostic.message}`

        try {
            await navigator.clipboard.writeText(text)
            setCopied(true)
            setTimeout(() => setCopied(false), 2000)
        } catch {
            // Fallback for older browsers
            console.error('Failed to copy to clipboard')
        }
    }, [diagnostic])

    return (
        <div
            className={`border-l-4 ${styles.border} ${styles.bg} p-3 mb-2 rounded-r`}
        >
            <div className="flex items-start justify-between gap-2">
                <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 text-xs text-grey-70 mb-1">
                        <span className={styles.icon}>
                            {getSeverityIcon(diagnostic.severity)}
                        </span>
                        <span
                            className={`uppercase font-semibold ${styles.icon}`}
                        >
                            {diagnostic.severity}
                        </span>
                        <span className="truncate" title={diagnostic.file}>
                            {fileName}
                            {diagnostic.line && `:${diagnostic.line}`}
                            {diagnostic.column && `:${diagnostic.column}`}
                        </span>
                    </div>
                    <p className="text-sm text-grey-30 break-words">
                        {diagnostic.message}
                    </p>
                </div>
                <button
                    onClick={handleCopy}
                    className={`flex-shrink-0 p-1.5 rounded transition-colors cursor-pointer ${
                        copied
                            ? 'text-green bg-green/20'
                            : 'text-grey-70 hover:text-white hover:bg-grey-120'
                    }`}
                    title={copied ? 'Copied!' : 'Copy diagnostic'}
                >
                    {copied ? <CheckIcon /> : <CopyIcon />}
                </button>
            </div>
        </div>
    )
}

interface FilterButtonProps {
    active: boolean
    onClick: () => void
    icon: React.ReactNode
    count: number
    label: string
    colorClass: string
}

const FilterButton: React.FC<FilterButtonProps> = ({
    active,
    onClick,
    icon,
    count,
    label,
    colorClass,
}) => (
    <button
        onClick={onClick}
        className={`flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium transition-all cursor-pointer ${
            active
                ? `${colorClass} text-white`
                : 'bg-grey-120 text-grey-70 hover:text-white'
        }`}
        title={`${active ? 'Hide' : 'Show'} ${label}`}
    >
        {icon}
        <span>{count}</span>
        <span className="hidden sm:inline">{label}</span>
    </button>
)

// Severity order for sorting: errors first, then warnings, then hints
const severityOrder: Record<DiagnosticItem['severity'], number> = {
    error: 0,
    warning: 1,
    hint: 2,
}

export const DiagnosticsHud: React.FC<{ embedded?: boolean }> = ({
    embedded = false,
}) => {
    const {
        isHudOpen,
        setHudOpen,
        diagnostics,
        status,
        errors,
        warnings,
        hints,
        filters,
        toggleFilter,
        showAllFilters,
    } = useDiagnosticsStore()
    const listRef = useRef<HTMLDivElement>(null)
    const prevDiagnosticsLength = useRef(diagnostics.length)

    // Filter and sort diagnostics
    const filteredDiagnostics = useMemo(() => {
        return diagnostics
            .filter((d) => {
                if (d.severity === 'error' && !filters.errors) return false
                if (d.severity === 'warning' && !filters.warnings) return false
                if (d.severity === 'hint' && !filters.hints) return false
                return true
            })
            .sort(
                (a, b) => severityOrder[a.severity] - severityOrder[b.severity]
            )
    }, [diagnostics, filters])

    // Check if all filters are active
    const anyFilterInactive =
        !filters.errors || !filters.warnings || !filters.hints

    // Auto-scroll to bottom when new diagnostics are added
    useEffect(() => {
        if (
            diagnostics.length > prevDiagnosticsLength.current &&
            listRef.current
        ) {
            listRef.current.scrollTop = listRef.current.scrollHeight
        }
        prevDiagnosticsLength.current = diagnostics.length
    }, [diagnostics.length])

    // Handle escape key to close
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape' && isHudOpen) {
                setHudOpen(false)
            }
        }

        document.addEventListener('keydown', handleKeyDown)
        return () => document.removeEventListener('keydown', handleKeyDown)
    }, [isHudOpen, setHudOpen])

    if (!embedded && !isHudOpen) {
        return null
    }

    return (
        <div
            className={`bg-grey-140 border-t border-grey-120 shadow-2xl flex flex-col min-h-0 ${embedded ? '' : 'fixed bottom-0 left-0 right-0'}`}
            style={
                embedded
                    ? { height: '100%' }
                    : { height: '300px', zIndex: 9998 }
            }
        >
            {/* Header */}
            <div className="flex items-center justify-between px-4 py-2 bg-grey-130 border-b border-grey-120">
                <div className="flex items-center gap-4">
                    <h3 className="text-white font-semibold">
                        Build Diagnostics
                    </h3>
                    {status === 'building' && (
                        <span className="text-sm text-blue-elastic animate-pulse">
                            Building...
                        </span>
                    )}
                </div>

                {/* Filter toggles */}
                <div className="flex items-center gap-2">
                    {anyFilterInactive && (
                        <button
                            onClick={showAllFilters}
                            className="px-2.5 py-1 rounded-full text-xs font-medium bg-grey-120 text-grey-70 hover:text-white transition-colors cursor-pointer"
                            title="Show all"
                        >
                            All
                        </button>
                    )}
                    {errors > 0 && (
                        <FilterButton
                            active={filters.errors}
                            onClick={() => toggleFilter('errors')}
                            icon={<ErrorIcon />}
                            count={errors}
                            label="errors"
                            colorClass="bg-red"
                        />
                    )}
                    {warnings > 0 && (
                        <FilterButton
                            active={filters.warnings}
                            onClick={() => toggleFilter('warnings')}
                            icon={<WarningIcon />}
                            count={warnings}
                            label="warnings"
                            colorClass="bg-yellow"
                        />
                    )}
                    {hints > 0 && (
                        <FilterButton
                            active={filters.hints}
                            onClick={() => toggleFilter('hints')}
                            icon={<HintIcon />}
                            count={hints}
                            label="hints"
                            colorClass="bg-blue-elastic"
                        />
                    )}
                    {errors === 0 &&
                        warnings === 0 &&
                        hints === 0 &&
                        status === 'complete' && (
                            <span className="text-sm text-green">
                                No issues found
                            </span>
                        )}

                    <div className="w-px h-5 bg-grey-120 mx-1" />

                    <button
                        onClick={() => setHudOpen(false)}
                        className="text-grey-70 hover:text-white transition-colors p-1 rounded hover:bg-grey-120 cursor-pointer"
                        aria-label="Close diagnostics panel"
                    >
                        <CloseIcon />
                    </button>
                </div>
            </div>

            {/* Diagnostics list */}
            <div
                ref={listRef}
                className={`overflow-y-auto p-4 ${embedded ? 'flex-1 min-h-0' : ''}`}
                style={embedded ? undefined : { height: 'calc(100% - 48px)' }}
            >
                {filteredDiagnostics.length === 0 ? (
                    <div className="flex items-center justify-center h-full text-grey-80">
                        {status === 'building'
                            ? 'Waiting for diagnostics...'
                            : diagnostics.length === 0
                              ? 'No diagnostics to display'
                              : 'No diagnostics match the current filters'}
                    </div>
                ) : (
                    filteredDiagnostics.map((diagnostic) => (
                        <DiagnosticRow
                            key={diagnostic.id}
                            diagnostic={diagnostic}
                        />
                    ))
                )}
            </div>
        </div>
    )
}
