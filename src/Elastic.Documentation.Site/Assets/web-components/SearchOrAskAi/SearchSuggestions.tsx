/** @jsxImportSource @emotion/react */
import { useModalActions } from './modal.store'
import {
    EuiButton,
    EuiSpacer,
    EuiText,
    EuiTextTruncate,
    useEuiTheme,
} from '@elastic/eui'
import { css } from '@emotion/react'
import htmx from 'htmx.org'
import * as React from 'react'
import { useEffect } from 'react'

export interface Suggestion {
    title: string
    url: string
}

interface Props {
    suggestions: Suggestion[]
}

export const SearchSuggestions = (props: Props) => {
    return (
        <>
            <EuiText size="xs">Suggested pages</EuiText>
            <EuiSpacer size="s" />
            {props.suggestions.map((suggestion) => (
                <Button key={suggestion.url} suggestion={suggestion} />
            ))}
        </>
    )
}

interface ButtonProps {
    suggestion: Suggestion
}

const Button = ({ suggestion }: ButtonProps) => {
    const { closeModal } = useModalActions()
    const { euiTheme } = useEuiTheme()
    const buttonCss = css`
        border: none;
        & > span {
            justify-content: flex-start;
        }
        svg {
            color: ${euiTheme.colors.textSubdued};
        }
    `
    const ref = React.useRef<HTMLAnchorElement>(null)
    useEffect(() => {
        if (ref.current) {
            htmx.process(ref.current)
        }
    }, [ref])

    useEffect(() => {
        // Htmx doesnt have a type definition
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const handleHtmxAfterRequest = (event: any) => {
            // Make sure to only close the model if the request
            // was from this button
            if (event.detail.requestConfig.elt === ref.current) {
                closeModal()
            }
        }
        document.addEventListener('htmx:afterRequest', handleHtmxAfterRequest)
        return () =>
            document.removeEventListener(
                'htmx:afterRequest',
                handleHtmxAfterRequest
            )
    }, [suggestion.url])
    const [selectOob, setSelectOob] = React.useState<string>('#main-container')
    useEffect(() => {
        const getFirstSegment = (path: string) =>
            path.replace('/docs/', '/').split('/')[1]
        const firstSegmentCurrentUrl = getFirstSegment(window.location.pathname)
        const firstSegmentFromSuggestionUrl = getFirstSegment(suggestion.url)
        setSelectOob(
            firstSegmentCurrentUrl === firstSegmentFromSuggestionUrl
                ? '#content-container,#toc-nav'
                : '#main-container'
        )
    }, [window.location.pathname])
    return (
        <EuiButton
            buttonRef={ref}
            iconType="document"
            color="text"
            fullWidth
            size="s"
            css={buttonCss}
            href={suggestion.url}
            hx-select-oob={selectOob}
        >
            {suggestion.title}{' '}
            <EuiText
                css={css`
                    width: 100%;
                `}
                textAlign="left"
                size="s"
                color="subdued"
            >
                <EuiTextTruncate text={suggestion.url} truncation="end" />
            </EuiText>
        </EuiButton>
    )
}
