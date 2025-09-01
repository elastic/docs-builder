import { AskAiSuggestions } from './AskAi/AskAiSuggestions'
import { SearchSuggestions } from './Search/SearchSuggestions'
import { useSearchTerm } from './search.store'
import { EuiHorizontalRule } from '@elastic/eui'
import * as React from 'react'

export function Suggestions() {
    return (
        <>
            <AskAiSuggestions
                suggestions={[
                    { question: 'What is an index template?' },
                    { question: 'What is semantic search?' },
                    { question: 'How do I create an index?' },
                ]}
            />
        </>
    )
}
