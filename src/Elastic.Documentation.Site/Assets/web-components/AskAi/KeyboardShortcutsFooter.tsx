import { availableIcons } from '../../eui-icons-cache'
import { EuiIcon, EuiText, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'
import React from 'react'

export interface KeyboardShortcut {
    keys: string[]
    label: string
}

interface KeyboardShortcutsFooterProps {
    shortcuts: KeyboardShortcut[]
}

export const KeyboardShortcutsFooter = ({
    shortcuts,
}: KeyboardShortcutsFooterProps) => {
    const { euiTheme } = useEuiTheme()
    return (
        <div
            css={css`
                display: flex;
                align-items: center;
                justify-content: center;
                gap: ${euiTheme.size.l};
                border-top: 1px solid ${euiTheme.colors.borderBaseSubdued};
                background-color: ${euiTheme.colors.backgroundBasePlain};
                border-bottom-right-radius: ${euiTheme.size.s};
                border-bottom-left-radius: ${euiTheme.size.s};
                padding-inline: ${euiTheme.size.base};
                padding-block: ${euiTheme.size.m};
            `}
        >
            {shortcuts.map((shortcut, index) => (
                <KeyboardIconsWithLabel
                    key={index}
                    keys={shortcut.keys}
                    label={shortcut.label}
                />
            ))}
        </div>
    )
}

const KeyboardKey = ({ children }: { children: React.ReactNode }) => {
    const { euiTheme } = useEuiTheme()
    return (
        <span
            css={css`
                display: inline-flex;
                justify-content: center;
                align-items: center;
                background-color: ${euiTheme.colors.backgroundBasePlain};
                border: 1px solid ${euiTheme.colors.borderBasePlain};
                min-width: ${euiTheme.size.l};
                height: ${euiTheme.size.l};
                border-radius: ${euiTheme.border.radius.small};
                padding-inline: ${euiTheme.size.xs};
            `}
        >
            {children}
        </span>
    )
}

const KeyboardIcon = ({ type }: { type: string }) => {
    const { euiTheme } = useEuiTheme()
    return (
        <KeyboardKey>
            {availableIcons.includes(type) ? (
                <EuiIcon type={type} size="s" />
            ) : (
                <span
                    css={css`
                        margin-inline: ${euiTheme.size.xs};
                        font-size: ${euiTheme.size.m};
                        line-height: ${euiTheme.size.base};
                        display: inline-block;
                    `}
                >
                    {type}
                </span>
            )}
        </KeyboardKey>
    )
}

const KeyboardIconsWithLabel = ({
    keys,
    label,
}: {
    keys: string[]
    label: string
}) => {
    const { euiTheme } = useEuiTheme()
    return (
        <span
            css={css`
                display: flex;
                align-items: center;
                gap: ${euiTheme.size.xs};
                color: #1d2a3e;
            `}
        >
            <span
                css={css`
                    display: flex;
                    gap: ${euiTheme.size.xs};
                `}
            >
                {keys.map((key, index) => (
                    <KeyboardIcon type={key} key={key + index} />
                ))}
            </span>
            <EuiText size="xs">{label}</EuiText>
        </span>
    )
}
