import { EuiLoadingSpinner, useEuiTheme } from '@elastic/eui'
import { css } from '@emotion/react'

const CustomSearchIcon = () => {
    const { euiTheme } = useEuiTheme()
    
    return (
        <svg
            width="16"
            height="16"
            viewBox="0 0 16 16"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            css={css`
                color: ${euiTheme.colors.textDisabled};
                flex-shrink: 0;
            `}
        >
            <path
                d="M7.87891 8.87891C9.05048 7.70735 10.9495 7.70735 12.1211 8.87891C13.172 9.93001 13.2802 11.5668 12.4453 12.7383L13.8535 14.1465L13.1465 14.8535L11.7383 13.4453C10.5668 14.2802 8.93001 14.172 7.87891 13.1211C6.70735 11.9495 6.70734 10.0505 7.87891 8.87891ZM5 13H2V12H5V13ZM11.4141 9.58594C10.633 8.80491 9.36698 8.80491 8.58594 9.58594C7.8049 10.367 7.8049 11.633 8.58594 12.4141C9.367 13.1949 10.6331 13.195 11.4141 12.4141C12.195 11.6331 12.1949 10.367 11.4141 9.58594ZM6 10H2V9H6V10ZM14 7H2V6H14V7ZM14 4H2V3H14V4Z"
                fill="currentColor"
            />
        </svg>
    )
}

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
                    left: ${euiTheme.base * 0.75}px;
                    display: flex;
                    align-items: center;
                    pointer-events: none;
                `}
            >
                {isLoading ? (
                    <EuiLoadingSpinner size="m" />
                ) : (
                    <CustomSearchIcon />
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
                    padding: calc(${euiTheme.size.s} + 2px) ${euiTheme.size.m};
                    padding-left: 34px;
                    padding-right: calc(${euiTheme.size.m} + 2ch + ${euiTheme.size.m});
                    border: 1px solid ${euiTheme.colors.borderBasePlain};
                    border-radius: ${euiTheme.border.radius.medium};
                    background: ${euiTheme.colors.backgroundBasePlain};
                    font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
                    line-height: ${euiTheme.base * 1.25}px;
                    color: ${euiTheme.colors.textParagraph};
                    outline: none;

                    &::placeholder {
                        color: ${euiTheme.colors.textDisabled};
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
                    display: inline-flex;
                    align-items: center;
                    pointer-events: none;
                    color: ${euiTheme.colors.textDisabled};
                    font-size: ${euiTheme.font.scale.s * euiTheme.base}px;
                    line-height: ${euiTheme.base * 1.25}px;
                `}
            >
                ⌘K
            </span>
        </div>
    )
}
