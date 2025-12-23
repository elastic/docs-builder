import { EuiIcon, EuiLoadingSpinner, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'

export interface SearchInputProps {
    inputRef: React.RefObject<HTMLInputElement>
    value: string
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => void
    onFocus: () => void
    onBlur: (e: React.FocusEvent) => void
    onKeyDown: (e: React.KeyboardEvent<HTMLInputElement>) => void
    disabled: boolean
    isLoading: boolean
}

export const SearchInput = ({
    inputRef,
    value,
    onChange,
    onFocus,
    onBlur,
    onKeyDown,
    disabled,
    isLoading,
}: SearchInputProps) => {
    const { euiTheme } = useEuiTheme()

    return (
        <div
            css={css`
                position: relative;
                display: flex;
                align-items: center;
            `}
        >
            <span
                css={css`
                    position: absolute;
                    left: ${euiTheme.size.m};
                    display: flex;
                    align-items: center;
                    pointer-events: none;
                `}
            >
                {isLoading ? (
                    <EuiLoadingSpinner size="s" />
                ) : (
                    <EuiIcon type="search" size="m" color="subdued" />
                )}
            </span>

            <input
                ref={inputRef}
                type="text"
                placeholder="Search in Docs"
                value={value}
                onChange={onChange}
                onFocus={onFocus}
                onBlur={onBlur}
                onKeyDown={onKeyDown}
                disabled={disabled}
                css={css`
                    width: 100%;
                    padding: ${euiTheme.size.s} ${euiTheme.size.m};
                    padding-left: ${euiTheme.size.xxl};
                    padding-right: 62px;
                    border: 1px solid ${euiTheme.colors.borderBaseSubdued};
                    border-radius: ${euiTheme.border.radius.medium};
                    background: ${euiTheme.colors.backgroundBasePlain};
                    font-size: ${euiTheme.font.scale.m * euiTheme.base}px;
                    color: ${euiTheme.colors.textParagraph};
                    outline: none;

                    &::placeholder {
                        color: ${euiTheme.colors.textSubdued};
                    }

                    &:focus {
                        border-color: ${euiTheme.colors.primary};
                    }
                `}
            />

            <span
                css={css`
                    position: absolute;
                    right: ${euiTheme.size.m};
                    display: flex;
                    align-items: center;
                    pointer-events: none;
                    height: ${euiTheme.size.l};
                    padding: 0 ${euiTheme.size.m};
                    gap: ${euiTheme.size.s};
                    border-radius: ${euiTheme.border.radius.small};
                    border: 1px solid ${euiTheme.colors.borderBasePlain};
                    background: ${euiTheme.colors.backgroundBasePlain};
                    color: ${euiTheme.colors.textDisabled};
                    font-size: ${euiTheme.size.m};
                    font-weight: ${euiTheme.font.weight.regular};
                    line-height: ${euiTheme.size.base};
                `}
            >
                ⌘K
            </span>
        </div>
    )
}
