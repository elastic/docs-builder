/** @jsxImportSource @emotion/react */
import { useChatActions } from '../AskAi/chat.store'
import { useModalActions, useCooldown } from '../modal.store'
import { SearchOrAskAiErrorCallout } from '../SearchOrAskAiErrorCallout'
import { SearchResults } from './SearchResults'
import { useSearchActions, useSearchTerm } from './search.store'
import { EuiFieldText, EuiSpacer, EuiButton, EuiButtonIcon } from '@elastic/eui'
import { css } from '@emotion/react'
import { useCallback, useRef, useState, useEffect } from 'react'

const askAiButtonStyles = css`
    font-weight: bold;
`

export const Search = () => {
    const searchTerm = useSearchTerm()
    const { setSearchTerm } = useSearchActions()
    const { submitQuestion, clearChat } = useChatActions()
    const { setModalMode } = useModalActions()
    const countdown = useCooldown()
    const inputRef = useRef<HTMLInputElement>(null)
    const [inputValue, setInputValue] = useState(searchTerm)

    const handleSearch = useCallback(() => {
        if (searchTerm.trim()) {
            // Prevent submission during countdown
            if (countdown !== null && countdown > 0) {
                return
            }
            // Always start a new conversation
            clearChat()
            submitQuestion(searchTerm)
            setModalMode('askAi')
        }
    }, [searchTerm, countdown, clearChat, submitQuestion, setModalMode])

    // Sync inputValue with searchTerm from store (when cleared externally)
    useEffect(() => {
        if (searchTerm === '' && inputValue !== '') {
            setInputValue('')
        }
    }, [searchTerm, inputValue])

    return (
        <>
            <EuiSpacer size="m" />
            {!searchTerm.trim() && <SearchOrAskAiErrorCallout error={null} />}
            <div
                css={css`
                    position: relative;
                `}
            >
                <EuiFieldText
                    autoFocus
                    inputRef={inputRef}
                    fullWidth
                    placeholder="Search the docs as you type"
                    value={inputValue}
                    onChange={(e) => {
                        const newValue = e.target.value
                        setInputValue(newValue)
                        setSearchTerm(newValue)
                    }}
                    onKeyDown={(e) => {
                        if (e.key === 'Enter') {
                            handleSearch()
                        }
                    }}
                    disabled={countdown !== null && countdown > 0}
                />
                <EuiButtonIcon
                    aria-label="Search"
                    css={css`
                        position: absolute;
                        right: 8px;
                        top: 50%;
                        transform: translateY(-50%);
                        border-radius: 9999px;
                    `}
                    color="primary"
                    iconType="sortUp"
                    display={inputValue.trim() ? 'fill' : 'base'}
                    onClick={handleSearch}
                    disabled={countdown !== null && countdown > 0}
                />
            </div>
            {searchTerm && (
                <>
                    <EuiSpacer size="s" />
                    <AskAiButton
                        term={searchTerm}
                        onAsk={() => {
                            // Prevent submission during countdown
                            if (countdown !== null && countdown > 0) {
                                return
                            }
                            clearChat()
                            if (searchTerm.trim()) submitQuestion(searchTerm)
                            setModalMode('askAi')
                        }}
                        disabled={countdown !== null && countdown > 0}
                    />
                </>
            )}

            <EuiSpacer size="m" />
            <SearchResults />
        </>
    )
}

const AskAiButton = ({ term, onAsk, disabled }: { term: string; onAsk: () => void; disabled?: boolean }) => {
    return (
        <EuiButton iconType="newChat" fullWidth onClick={onAsk} disabled={disabled}>
            Ask AI about <span css={askAiButtonStyles}>"{term}"</span>
        </EuiButton>
    )
}
