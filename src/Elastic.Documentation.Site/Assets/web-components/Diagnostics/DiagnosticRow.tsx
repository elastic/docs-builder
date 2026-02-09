import { SEVERITY_CONFIG, DiagnosticItem } from './diagnostics.store'
import {
    EuiButtonIcon,
    EuiFlexGroup,
    EuiFlexItem,
    EuiIcon,
    EuiText,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import * as React from 'react'
import { useCallback, useState } from 'react'

const formatLocation = (diagnostic: DiagnosticItem): string => {
    // return diagnostic.file;
    const fileName = diagnostic.file
    let location = fileName
    if (diagnostic.line != null) location += `:${diagnostic.line}`
    if (diagnostic.column != null) location += `:${diagnostic.column}`
    return location
}

const formatClipboardText = (diagnostic: DiagnosticItem): string => {
    const location = diagnostic.line
        ? `${diagnostic.file}:${diagnostic.line}${diagnostic.column ? `:${diagnostic.column}` : ''}`
        : diagnostic.file
    return `[${diagnostic.severity.toUpperCase()}] ${location}: ${diagnostic.message}`
}

export const DiagnosticRow: React.FC<{ diagnostic: DiagnosticItem }> = ({
    diagnostic,
}) => {
    const [copied, setCopied] = useState(false)
    const { euiTheme } = useEuiTheme()
    const { iconType, color } = SEVERITY_CONFIG[diagnostic.severity]

    const borderColor =
        color === 'danger'
            ? euiTheme.colors.danger
            : color === 'warning'
              ? euiTheme.colors.warning
              : euiTheme.colors.primary

    const handleCopy = useCallback(async () => {
        try {
            await navigator.clipboard.writeText(formatClipboardText(diagnostic))
            setCopied(true)
            setTimeout(() => setCopied(false), 2000)
        } catch {
            console.error('Failed to copy to clipboard')
        }
    }, [diagnostic])

    const rowCss = css`
        border-left: 3px solid ${borderColor};
        border-bottom: 1px solid ${euiTheme.border.color};
        padding: ${euiTheme.size.m} ${euiTheme.size.m};
    `

    const contentCss = css`
        min-width: 0;
    `

    const locationCss = css`
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    `

    const messageCss = css`
        font-family: ${euiTheme.font.familyCode};
        word-break: break-word;
        margin-top: ${euiTheme.size.xs};
    `

    return (
        <div css={rowCss}>
            <EuiFlexGroup
                alignItems="flexStart"
                justifyContent="spaceBetween"
                gutterSize="s"
                responsive={false}
            >
                <EuiFlexItem grow={1} css={contentCss}>
                    <EuiFlexGroup
                        alignItems="center"
                        gutterSize="s"
                        wrap
                        responsive={false}
                    >
                        <EuiIcon type={iconType} color={color} size="s" />
                        <EuiText
                            size="xs"
                            color={color}
                            css={css`
                                ffont-weight: 500;
                                letter-spacing: 0.05em;
                                font-size: 0.66em;
                            `}
                        >
                            {diagnostic.severity.toUpperCase()}
                        </EuiText>
                        <EuiText
                            size="xs"
                            color="subdued"
                            css={locationCss}
                            title={diagnostic.file}
                        >
                            {formatLocation(diagnostic)}
                        </EuiText>
                    </EuiFlexGroup>
                    <EuiText size="xs" color="white" css={messageCss}>
                        {diagnostic.message}
                    </EuiText>
                </EuiFlexItem>
                <EuiFlexItem grow={false}>
                    <EuiButtonIcon
                        iconType={copied ? 'check' : 'copy'}
                        color={copied ? 'success' : 'text'}
                        aria-label={copied ? 'Copied' : 'Copy diagnostic'}
                        onClick={handleCopy}
                    />
                </EuiFlexItem>
            </EuiFlexGroup>
        </div>
    )
}
