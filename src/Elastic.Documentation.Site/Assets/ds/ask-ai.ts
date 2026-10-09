// The AI assistant panel. The server renders the dialog hidden; this streams the conversation from the AI endpoint and draws it.
// The stream is injected so the panel can be tested without a network.
import { config } from '../config'
import { askAiConfig } from '../web-components/AskAi/askAi.config'
import { markdownRenderer } from '../web-components/shared/markdownRenderer'
import { fetchEventSource } from '@microsoft/fetch-event-source'
import DOMPurify from 'dompurify'

export interface AskAiEventLike {
    type: string
    content?: string
    fullContent?: string
    conversationId?: string
    message?: string
}

export interface StreamRequest {
    message: string
    conversationId: string | null
    signal: AbortSignal
    onEvent: (event: AskAiEventLike) => void
    onError: (error: Error) => void
    onClose: () => void
}

export type AskAiStream = (request: StreamRequest) => Promise<void>

async function sha256(data: string): Promise<string> {
    const buffer = await crypto.subtle.digest(
        'SHA-256',
        new TextEncoder().encode(data)
    )
    return Array.from(new Uint8Array(buffer))
        .map((b) => b.toString(16).padStart(2, '0'))
        .join('')
}

export const streamFromApi: AskAiStream = async (request) => {
    const body = JSON.stringify({
        message: request.message,
        ...(request.conversationId && {
            conversationId: request.conversationId,
        }),
    })
    await fetchEventSource(`${config.apiBasePath}/v1/ask-ai/stream`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'x-amz-content-sha256': await sha256(body),
            'X-AI-Provider': askAiConfig.defaultAiProvider,
        },
        body,
        signal: request.signal,
        openWhenHidden: true,
        onopen: async (response) => {
            if (!response.ok)
                throw new Error(`Request failed (${response.status})`)
        },
        onmessage: (message) => {
            if (!message.data?.trim()) return
            try {
                request.onEvent(JSON.parse(message.data) as AskAiEventLike)
            } catch {
                // an unreadable event is skipped; the stream carries on
            }
        },
        onerror: (error) => {
            request.onError(
                error instanceof Error ? error : new Error('Connection error')
            )
            throw error
        },
        onclose: () => request.onClose(),
    })
}

export function renderAnswer(markdown: string): string {
    return DOMPurify.sanitize(
        markdownRenderer.parse(markdown, { async: false })
    )
}

export function initAskAi(
    scope: ParentNode = document,
    stream: AskAiStream = streamFromApi
) {
    scope
        .querySelectorAll<HTMLDialogElement>(
            'dialog[data-ask-ai]:not([data-ask-ai-bound])'
        )
        .forEach((dialog) => {
            if (typeof dialog.showModal !== 'function') return
            dialog.dataset.askAiBound = ''
            bindAskAi(dialog, stream)
        })
}

function bindAskAi(dialog: HTMLDialogElement, stream: AskAiStream) {
    const q = <T extends HTMLElement>(selector: string) =>
        dialog.querySelector<T>(selector)!
    const form = q<HTMLFormElement>('[data-ask-ai-form]')
    const input = q<HTMLTextAreaElement>('[data-ask-ai-input]')
    const list = q('[data-ask-ai-messages]')
    const intro = q('[data-ask-ai-intro]')
    const send = q<HTMLButtonElement>('[data-ask-ai-send]')
    const stop = q<HTMLButtonElement>('[data-ask-ai-stop]')
    const template = q<HTMLTemplateElement>('template[data-ask-ai-message]')

    let conversationId: string | null = null
    let controller: AbortController | null = null

    q('[data-ask-ai-description]').textContent =
        askAiConfig.assistantDescription
    input.placeholder = askAiConfig.inputPlaceholder
    q('[data-ask-ai-suggestions]').replaceChildren(
        ...askAiConfig.suggestions.slice(0, 5).map((suggestion) => {
            const item = document.createElement('li')
            const button = document.createElement('button')
            button.type = 'button'
            button.className = 'ask-ai__suggestion'
            button.textContent = suggestion.question
            button.addEventListener('click', () => ask(suggestion.question))
            item.append(button)
            return item
        })
    )

    document
        .querySelectorAll<HTMLElement>(`[data-ask-ai-open="${dialog.id}"]`)
        .forEach((trigger) => {
            trigger.hidden = false
            trigger.addEventListener('click', () => {
                dialog.showModal()
                input.focus()
            })
        })
    dialog.addEventListener('click', (event) => {
        if (event.target === dialog) dialog.close()
    })

    function addMessage(role: 'user' | 'assistant', text: string) {
        const item = (
            template.content.cloneNode(true) as DocumentFragment
        ).querySelector<HTMLElement>('li')!
        item.dataset.role = role
        item.querySelector('.ask-ai__author')!.textContent =
            role === 'user' ? 'You' : 'Assistant'
        const body = item.querySelector<HTMLElement>('.ask-ai__text')!
        if (role === 'user') body.textContent = text
        list.append(item)
        item.scrollIntoView?.({ block: 'end' })
        return { item, body }
    }

    function setBusy(busy: boolean) {
        send.hidden = busy
        stop.hidden = !busy
        input.disabled = busy
    }

    async function ask(question: string) {
        const text = question.trim()
        if (!text || controller) return
        intro.hidden = true
        addMessage('user', text)
        const answer = addMessage('assistant', '')
        answer.item.dataset.state = 'pending'
        input.value = ''
        setBusy(true)
        controller = new AbortController()
        let received = ''

        const fail = (message: string) => {
            answer.item.dataset.state = 'error'
            answer.body.textContent = message
        }

        try {
            await stream({
                message: text,
                conversationId,
                signal: controller.signal,
                onEvent: (event) => {
                    if (
                        event.type === 'conversation_start' &&
                        event.conversationId
                    )
                        conversationId = event.conversationId
                    else if (event.type === 'message_chunk') {
                        received += event.content ?? ''
                        delete answer.item.dataset.state
                        answer.body.innerHTML = renderAnswer(received)
                    } else if (
                        event.type === 'message_complete' &&
                        event.fullContent
                    ) {
                        received = event.fullContent
                        delete answer.item.dataset.state
                        answer.body.innerHTML = renderAnswer(received)
                    } else if (event.type === 'error')
                        fail(event.message ?? 'Something went wrong.')
                },
                onError: (error) => {
                    if (error.name !== 'AbortError')
                        fail(error.message || 'Something went wrong.')
                },
                onClose: () => undefined,
            })
        } catch (error) {
            if (
                (error as Error).name !== 'AbortError' &&
                !answer.body.textContent
            )
                fail('Something went wrong. Try again.')
        } finally {
            if (answer.item.dataset.state === 'pending') {
                delete answer.item.dataset.state
                if (!answer.body.textContent)
                    answer.body.textContent = 'Stopped.'
            }
            controller = null
            setBusy(false)
            input.focus()
        }
    }

    form.addEventListener('submit', (event) => {
        event.preventDefault()
        void ask(input.value)
    })
    input.addEventListener('keydown', (event) => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault()
            void ask(input.value)
        }
    })
    stop.addEventListener('click', () => controller?.abort())
    q('[data-ask-ai-new]').addEventListener('click', () => {
        controller?.abort()
        conversationId = null
        list.replaceChildren()
        intro.hidden = false
    })
}
