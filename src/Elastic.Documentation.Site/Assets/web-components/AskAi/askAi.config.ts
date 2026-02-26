import { config } from '../../config'

interface AskAiSuggestion {
    question: string
}

interface AskAiConfig {
    assistantName: string
    assistantDescription: string
    inputPlaceholder: string
    defaultAiProvider: 'AgentBuilder' | 'LlmGateway'
    forceAiProvider: boolean
    suggestions: AskAiSuggestion[]
}

const codexConfig: AskAiConfig = {
    assistantName: 'Elastic Internal Docs AI Assistant',
    assistantDescription:
        "I'm here to help you navigate Elastic's internal documentation. Ask me anything about our internal processes, tools, and standards. How can I help?",
    inputPlaceholder: 'Ask me anything...',
    defaultAiProvider: 'AgentBuilder',
    forceAiProvider: true,
    suggestions: [
        { question: 'How do I get started with Elastic Internal Docs?' },
        {
            question:
                'How do I set up Elastic Internal Docs for my repository?',
        },
        {
            question:
                'How does Elastic Internal Docs assemble docs from multiple repositories?',
        },
        {
            question:
                'How do I migrate my docs from Docsmobile to Elastic Internal Docs?',
        },
        {
            question:
                'How do I configure Elastic Internal Docs CI previews for my documentation?',
        },
        {
            question:
                'What is an Elastic Internal Docs docset.yml and how do I configure it?',
        },
    ],
}

const defaultConfig: AskAiConfig = {
    assistantName: 'Elastic Docs AI Assistant',
    assistantDescription:
        "I'm here to help you find answers about Elastic, powered entirely by our technical documentation. How can I help?",
    inputPlaceholder: 'Ask the Elastic Docs AI Assistant',
    defaultAiProvider: 'LlmGateway',
    forceAiProvider: false,
    suggestions: [
        { question: 'How do I set up a data stream in Elasticsearch?' },
        { question: 'What are the best practices for indexing performance?' },
        { question: 'How can I create a dashboard in Kibana?' },
        {
            question:
                'What is the difference between a keyword and text field?',
        },
        { question: 'How do I configure machine learning jobs?' },
        { question: 'What are aggregations and how do I use them?' },
        {
            question:
                'How do I set up Elasticsearch security and authentication?',
        },
        { question: 'What are the different types of Elasticsearch queries?' },
        { question: 'How do I monitor cluster health and performance?' },
        {
            question:
                'What is the Elastic Stack and how do the components work together?',
        },
        { question: 'How do I create and manage Elasticsearch indices?' },
        { question: 'What are the best practices for Elasticsearch mapping?' },
        { question: 'How do I set up log shipping with Beats?' },
        {
            question:
                'What is APM and how do I use it for application monitoring?',
        },
        { question: 'How do I create custom visualizations in Kibana?' },
        { question: 'What are Elasticsearch snapshots and how do I use them?' },
        { question: 'How do I configure cross-cluster search?' },
        {
            question:
                'What are the different Elasticsearch node types and their roles?',
        },
    ],
}

export const askAiConfig =
    config.buildType === 'codex' ? codexConfig : defaultConfig
