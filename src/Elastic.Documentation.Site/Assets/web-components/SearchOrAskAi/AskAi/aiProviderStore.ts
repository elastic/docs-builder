import { create } from 'zustand'

type AiProvider = 'AgentBuilder' | 'LlmGateway'

interface AiProviderState {
    provider: AiProvider
    setProvider: (provider: AiProvider) => void
}

export const useAiProviderStore = create<AiProviderState>((set) => ({
    provider: 'LlmGateway', // Default to LLM Gateway
    setProvider: (provider: AiProvider) => {
        console.log(`[AI Provider] Switched to ${provider}`)
        set({ provider })
    },
}))

export const useAiProvider = () => useAiProviderStore((state) => state.provider)
