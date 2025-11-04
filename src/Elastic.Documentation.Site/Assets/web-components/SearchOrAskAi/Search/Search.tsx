/** @jsxImportSource @emotion/react */
import { useChatActions } from '../AskAi/chat.store'
import { useModalActions } from '../modal.store'
import { SearchResults } from './SearchResults'
import { useSearchActions, useSearchTerm } from './search.store'
import { EuiFieldSearch, EuiSpacer, EuiButton } from '@elastic/eui'
import { css } from '@emotion/react'
import { useState, useCallback } from 'react'

const askAiButtonStyles = css`
    font-weight: bold;
`

export const Search = () => {
    const searchTerm = useSearchTerm()
    const { setSearchTerm } = useSearchActions()
    const { submitQuestion, clearChat } = useChatActions()
    const { setModalMode } = useModalActions()
    const [countdown, setCountdown] = useState<number | null>(null)

    const handleCountdownChange = useCallback((newCountdown: number | null) => {
        setCountdown(newCountdown)
    }, [])
    return (
        <>
            <EuiSpacer size="m" />
            <EuiFieldSearch
                autoFocus
                fullWidth
                placeholder="Search the docs as you type"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                onSearch={(e) => {
                    if (e.trim()) {
                        // Prevent submission during countdown
                        if (countdown !== null && countdown > 0) {
                            return
                        }
                        // Always start a new conversation
                        clearChat()
                        submitQuestion(e)
                        setModalMode('askAi')
                    }
                }}
                isClearable
                disabled={countdown !== null && countdown > 0}
            />
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
            <SearchResults onCountdownChange={handleCountdownChange} />
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
