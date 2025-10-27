/** @jsxImportSource @emotion/react */
import { useAiProviderStore } from './aiProviderStore'
import { EuiRadioGroup } from '@elastic/eui'
import type { EuiRadioGroupOption } from '@elastic/eui'
import { css } from '@emotion/react'

const containerStyles = css`
    padding: 1rem;
    display: flex;
    justify-content: center;
`

const options: EuiRadioGroupOption[] = [
    {
        id: 'LlmGateway',
        label: 'LLM Gateway',
    },
    {
        id: 'AgentBuilder',
        label: 'Agent Builder',
    },
]

export const AiProviderSelector = () => {
    const { provider, setProvider } = useAiProviderStore()

    return (
        <div css={containerStyles}>
            <EuiRadioGroup
                options={options}
                idSelected={provider}
                onChange={(id) =>
                    setProvider(id as 'AgentBuilder' | 'LlmGateway')
                }
                name="aiProvider"
                legend={{
                    children: 'AI Provider',
                    display: 'visible',
                }}
            />
        </div>
    )
}
