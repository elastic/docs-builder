import { AskAiSuggestions } from './AskAi/AskAiSuggestions'
import { SearchSuggestions } from './Search/SearchSuggestions'
import { useSearchTerm } from './search.store'
import { EuiHorizontalRule } from '@elastic/eui'
import * as React from 'react'

export function Suggestions() {
    const searchTerm = useSearchTerm()
    return (
        <>
            {!searchTerm && (
                <SearchSuggestions
                    suggestions={[
                        { title: 'Contribute', url: '/contribute' },
                        { title: 'Syntax guide', url: '/syntax' },
                        { title: 'Quick reference', url: '/syntax/quick-ref' },
                        { title: 'Headings', url: '/syntax/headings' },
                    ]}
                />
            )}
            <EuiHorizontalRule margin="m" />
            <AskAiSuggestions
                suggestions={[
                    { question: 'How do I run Elasticsearch locally?' },
                    { question: 'How do I upgrade to 9.0?' },
                ]}
            />
        </>
    )
}
