import { useDiagnosticsStore } from './diagnostics.store'
import { EuiBadge } from '@elastic/eui'
import * as React from 'react'

// Animated spinner for building state
const BuildingSpinner: React.FC<{ className?: string }> = ({
    className = '',
}) => (
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

interface SegmentProps {
    icon: string
    count: number
    label: string
    gradientClass: string
    isFirst: boolean
    isLast: boolean
    type: 'danger' | 'warning' | 'primary' | 'success'
}

const Segment: React.FC<SegmentProps> = ({ icon, count, label, type }) => (
    <EuiBadge color={type} iconType={icon}>
        {count} {label}
    </EuiBadge>
    // <div
    //     className={`diagnostics-segment ${gradientClass} flex items-center gap-1.5 px-2.5 py-1.5 text-white text-sm font-medium ${isFirst ? 'rounded-l-full' : ''} ${isLast ? 'rounded-r-full' : ''}`}
    // >
    //     {icon}
    //     <span>{count}</span>
    //     <span className="hidden sm:inline">{label}</span>
    // </div>
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
        'diagnostics-pill rounded-full transition-all duration-200 text-white font-medium cursor-pointer'

    // Not connected yet, show connecting state
    if (!isConnected && status === 'idle') {
        return (
            <div
                onClick={handleClick}
                className={`${pillBase} diagnostics-pill-building flex items-center justify-center p-2`}
                title="Connecting to diagnostics..."
            >
                <BuildingSpinner />
            </div>
        )
    }

    // Building state with no prior issues - spinner only
    if (isBuilding && !hasIssues) {
        return (
            <div
                onClick={handleClick}
                className={`${pillBase} diagnostics-pill-building flex items-center gap-2 px-3 py-1.5 text-sm`}
                title="Building..."
            >
                <BuildingSpinner />
                <span>Building...</span>
            </div>
        )
    }

    // Has issues - show segmented pill
    if (hasIssues) {
        // Build segments array for items with count > 0
        const segments: {
            key: string
            icon: string
            count: number
            label: string
            gradientClass: string
            type: 'danger' | 'warning' | 'primary' | 'success'
        }[] = []

        if (errors > 0) {
            segments.push({
                key: 'errors',
                icon: 'error',
                count: errors,
                label: 'errors',
                gradientClass: 'diagnostics-segment-error',
                type: 'danger',
            })
        }
        if (warnings > 0) {
            segments.push({
                key: 'warnings',
                icon: 'warning',
                count: warnings,
                label: 'warnings',
                gradientClass: 'diagnostics-segment-warning',
                type: 'warning',
            })
        }
        if (hints > 0) {
            segments.push({
                key: 'hints',
                icon: 'info',
                count: hints,
                label: 'hints',
                gradientClass: 'diagnostics-segment-hint',
                type: 'primary',
            })
        }

        return (
            <div onClick={handleClick} title="Click to view diagnostics">
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
                        type={segment.type}
                    />
                ))}
            </div>
        )
    }

    // All good state - green gradient with checkmark
    return (
        <div
            onClick={handleClick}
            className={`${pillBase} diagnostics-pill-success flex items-center gap-2 px-3 py-1.5 text-sm`}
            title="All good! No issues found."
        >
            <CheckIcon />
            <span>All good!</span>
        </div>
    )
}
