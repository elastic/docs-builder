import * as React from 'react'
import { useDiagnosticsStore } from './diagnostics.store'

// Animated spinner for building state
const BuildingSpinner: React.FC<{ className?: string }> = ({ className = '' }) => (
    <svg
        className={`w-4 h-4 animate-spin ${className}`}
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2"
    >
        <circle cx="12" cy="12" r="10" strokeOpacity="0.25" />
        <path d="M12 2a10 10 0 0 1 10 10" strokeLinecap="round" />
    </svg>
)

// Checkmark icon
const CheckIcon: React.FC = () => (
    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
        <path
            fillRule="evenodd"
            d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
            clipRule="evenodd"
        />
    </svg>
)

// Error icon (X in circle)
const ErrorIcon: React.FC = () => (
    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
        <path
            fillRule="evenodd"
            d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
            clipRule="evenodd"
        />
    </svg>
)

// Warning icon (exclamation triangle)
const WarningIcon: React.FC = () => (
    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
        <path
            fillRule="evenodd"
            d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
            clipRule="evenodd"
        />
    </svg>
)

// Hint icon (info circle)
const HintIcon: React.FC = () => (
    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
        <path
            fillRule="evenodd"
            d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
            clipRule="evenodd"
        />
    </svg>
)

interface SegmentProps {
    icon: React.ReactNode
    count: number
    label: string
    gradientClass: string
    isFirst: boolean
    isLast: boolean
}

const Segment: React.FC<SegmentProps> = ({
    icon,
    count,
    label,
    gradientClass,
    isFirst,
    isLast,
}) => (
    <div
        className={`diagnostics-segment ${gradientClass} flex items-center gap-1.5 px-2.5 py-1.5 text-white text-sm font-medium ${isFirst ? 'rounded-l-full' : ''} ${isLast ? 'rounded-r-full' : ''}`}
    >
        {icon}
        <span>{count}</span>
        <span className="hidden sm:inline">{label}</span>
    </div>
)

export const DiagnosticsButton: React.FC = () => {
    const { status, errors, warnings, hints, toggleHud, isConnected } =
        useDiagnosticsStore()

    const hasIssues = errors > 0 || warnings > 0 || hints > 0
    const isBuilding = status === 'building'

    const handleClick = () => {
        toggleHud()
    }

    // Base pill styling for special states
    const pillBase =
        'diagnostics-pill rounded-full transition-all duration-200 text-white font-medium'

    // Not connected yet, show connecting state
    if (!isConnected && status === 'idle') {
        return (
            <button
                onClick={handleClick}
                className={`${pillBase} diagnostics-pill-building flex items-center justify-center p-2`}
                title="Connecting to diagnostics..."
            >
                <BuildingSpinner />
            </button>
        )
    }

    // Building state with no prior issues - spinner only
    if (isBuilding && !hasIssues) {
        return (
            <button
                onClick={handleClick}
                className={`${pillBase} diagnostics-pill-building flex items-center gap-2 px-3 py-1.5 text-sm`}
                title="Building..."
            >
                <BuildingSpinner />
                <span>Building...</span>
            </button>
        )
    }

    // Has issues - show segmented pill
    if (hasIssues) {
        // Build segments array for items with count > 0
        const segments: {
            key: string
            icon: React.ReactNode
            count: number
            label: string
            gradientClass: string
        }[] = []

        if (errors > 0) {
            segments.push({
                key: 'errors',
                icon: <ErrorIcon />,
                count: errors,
                label: 'errors',
                gradientClass: 'diagnostics-segment-error',
            })
        }
        if (warnings > 0) {
            segments.push({
                key: 'warnings',
                icon: <WarningIcon />,
                count: warnings,
                label: 'warnings',
                gradientClass: 'diagnostics-segment-warning',
            })
        }
        if (hints > 0) {
            segments.push({
                key: 'hints',
                icon: <HintIcon />,
                count: hints,
                label: 'hints',
                gradientClass: 'diagnostics-segment-hint',
            })
        }

        return (
            <button
                onClick={handleClick}
                className="diagnostics-segmented-pill flex items-stretch overflow-hidden rounded-full shadow-md animate-wobble"
                title="Click to view diagnostics"
            >
                {isBuilding && (
                    <div className="diagnostics-segment diagnostics-segment-building flex items-center px-2.5 py-1.5 rounded-l-full">
                        <BuildingSpinner />
                    </div>
                )}
                {segments.map((segment, index) => (
                    <Segment
                        key={segment.key}
                        icon={segment.icon}
                        count={segment.count}
                        label={segment.label}
                        gradientClass={segment.gradientClass}
                        isFirst={!isBuilding && index === 0}
                        isLast={index === segments.length - 1}
                    />
                ))}
            </button>
        )
    }

    // All good state - green gradient with checkmark
    return (
        <button
            onClick={handleClick}
            className={`${pillBase} diagnostics-pill-success flex items-center gap-2 px-3 py-1.5 text-sm`}
            title="All good! No issues found."
        >
            <CheckIcon />
            <span>All good!</span>
        </button>
    )
}
