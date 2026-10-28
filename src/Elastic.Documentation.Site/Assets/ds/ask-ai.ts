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

export function rateLimitMessage(retryAfter: string | null): string {
    const seconds = Number(retryAfter)
    return Number.isFinite(seconds) && seconds > 0
        ? `You have reached the limit for now. Try again in ${Math.ceil(seconds)} seconds.`
        : 'You have reached the limit for now. Try again in a moment.'
}

/** Streams one answer and calls back with the markdown received so far. Used by the search page, which has no conversation. */
export async function streamAnswer(
    message: string,
    signal: AbortSignal,
    onText: (markdown: string, done: boolean) => void,
    onFailure: (message: string) => void,
    stream: AskAiStream = streamFromApi
): Promise<void> {
    let received = ''
    try {
        await stream({
            message,
            conversationId: null,
            signal,
            onEvent: (event) => {
                if (event.type === 'message_chunk') {
                    received += event.content ?? ''
                    onText(received, false)
                } else if (
                    event.type === 'message_complete' &&
                    event.fullContent
                ) {
                    received = event.fullContent
                    onText(received, true)
                } else if (event.type === 'error')
                    onFailure(event.message ?? 'Something went wrong.')
            },
            onError: (error) => {
                if (error.name !== 'AbortError')
                    onFailure(error.message || 'Something went wrong.')
            },
            onClose: () => undefined,
        })
    } catch (error) {
        if ((error as Error).name !== 'AbortError' && !received)
            onFailure((error as Error).message || 'Something went wrong.')
    }
}

const historyKey = 'ask-ai-conversation'

interface StoredMessage {
    role: 'user' | 'assistant'
    text: string
    id?: string
    reaction?: Reaction
}

interface StoredConversation {
    conversationId: string | null
    messages: StoredMessage[]
}

export function readConversation(): StoredConversation {
    try {
        const raw = sessionStorage.getItem(historyKey)
        if (raw) return JSON.parse(raw) as StoredConversation
    } catch {
        // storage unavailable or unreadable: start a fresh conversation
    }
    return { conversationId: null, messages: [] }
}

function writeConversation(conversation: StoredConversation) {
    try {
        if (conversation.messages.length === 0)
            sessionStorage.removeItem(historyKey)
        else sessionStorage.setItem(historyKey, JSON.stringify(conversation))
    } catch {
        // storage unavailable: the conversation just isn't remembered
    }
}

export interface Reference {
    url: string
    title: string
    description: string
}

/** Splits the answer from the references the assistant appends as a comment. A reference block still streaming is hidden, not shown as text. */
export function splitReferences(content: string): {
    main: string
    references: Reference[]
} {
    const start = content.indexOf('<!--REFERENCES')
    if (start === -1) return { main: content, references: [] }
    const end = content.indexOf('-->', start)
    const main = content.substring(0, start).trim()
    if (end === -1) return { main, references: [] }
    let json = content.substring(start + '<!--REFERENCES'.length, end).trim()
    json = json
        .replace(/^```(json)?/, '')
        .replace(/```$/, '')
        .trim()
    try {
        const parsed: unknown = JSON.parse(json)
        return {
            main,
            references: Array.isArray(parsed)
                ? (parsed as Reference[]).filter(
                      (r) =>
                          typeof r.url === 'string' &&
                          typeof r.title === 'string'
                  )
                : [],
        }
    } catch {
        return { main, references: [] }
    }
}

export type Reaction = 'thumbsUp' | 'thumbsDown'

export async function sendReaction(
    messageId: string,
    conversationId: string,
    reaction: Reaction
): Promise<void> {
    await fetch(`${config.apiBasePath}/v1/ask-ai/message-feedback`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messageId, conversationId, reaction }),
    })
}

// References are built after the Markdown is sanitized, so a link must be checked here: only http(s) and site-relative addresses
// become links, never a javascript: or data: address from the model's answer.
export function safeHref(url: string): string | null {
    try {
        const parsed = new URL(url, window.location.origin)
        return parsed.protocol === 'http:' || parsed.protocol === 'https:'
            ? url
            : null
    } catch {
        return null
    }
}

/** Draws an answer: sanitized Markdown, then the references as links. Safe to call again as more text arrives. */
export function drawAnswer(body: HTMLElement, content: string) {
    const { main, references } = splitReferences(content)
    body.innerHTML = renderAnswer(main)
    if (references.length === 0) return
    const list = document.createElement('ul')
    list.className = 'ask-ai__references'
    references.forEach((reference) => {
        const item = document.createElement('li')
        const href = safeHref(reference.url)
        if (href) {
            const link = document.createElement('a')
            link.href = href
            link.textContent = reference.title
            item.append(link)
        } else item.append(document.createTextNode(reference.title))
        if (reference.description) {
            const description = document.createElement('span')
            description.textContent = ` ${reference.description}`
            item.append(description)
        }
        list.append(item)
    })
    body.append(list)
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

    const stored = readConversation()
    let conversationId: string | null = stored.conversationId
    const history: StoredMessage[] = [...stored.messages]
    let controller: AbortController | null = null
    // Bumped by New chat, so an answer that is still arriving when the conversation is cleared is not written back into it.
    let session = 0
    const save = () => writeConversation({ conversationId, messages: history })

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

    // Thumbs under a finished answer. The choice is kept with the conversation and sent once per change.
    function addFeedback(item: HTMLElement, messageId?: string) {
        if (!messageId || item.querySelector('.ask-ai__feedback')) return
        const stored = history.find((m) => m.id === messageId)
        const row = document.createElement('div')
        row.className = 'ask-ai__feedback'
        row.setAttribute('role', 'group')
        row.setAttribute('aria-label', 'Was this answer helpful?')
        ;(
            [
                ['thumbsUp', 'Helpful'],
                ['thumbsDown', 'Not helpful'],
            ] as const
        ).forEach(([reaction, label]) => {
            const button = document.createElement('button')
            button.type = 'button'
            button.className = 'ask-ai__reaction'
            button.textContent = label
            button.setAttribute(
                'aria-pressed',
                String(stored?.reaction === reaction)
            )
            button.addEventListener('click', () => {
                if (!conversationId || !stored || stored.reaction === reaction)
                    return
                stored.reaction = reaction
                row.querySelectorAll('button').forEach((b) =>
                    b.setAttribute('aria-pressed', String(b === button))
                )
                save()
                void sendReaction(messageId, conversationId, reaction).catch(
                    () => undefined
                )
            })
            row.append(button)
        })
        item.append(row)
    }

    // A saved conversation is drawn the way new answers are: with their references and with the reaction controls.
    history.forEach((message) => {
        const restored = addMessage(message.role, message.text)
        if (message.role !== 'assistant') return
        drawAnswer(restored.body, message.text)
        addFeedback(restored.item, message.id)
    })
    if (history.length > 0) intro.hidden = true

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
        history.push({ role: 'user', text })
        const answer = addMessage('assistant', '')
        answer.item.dataset.state = 'pending'
        input.value = ''
        setBusy(true)
        controller = new AbortController()
        const mine = session
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
                        drawAnswer(answer.body, received)
                    } else if (
                        event.type === 'message_complete' &&
                        event.fullContent
                    ) {
                        received = event.fullContent
                        delete answer.item.dataset.state
                        drawAnswer(answer.body, received)
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
            if (mine === session) {
                if (answer.item.dataset.state === 'pending') {
                    delete answer.item.dataset.state
                    if (!answer.body.textContent)
                        answer.body.textContent = 'Stopped.'
                }
                if (received) {
                    const id =
                        globalThis.crypto?.randomUUID?.() ?? `m-${Date.now()}`
                    history.push({ role: 'assistant', text: received, id })
                    addFeedback(answer.item, id)
                } else history.pop()
                save()
                controller = null
                setBusy(false)
                input.focus()
            }
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
        session += 1
        controller?.abort()
        controller = null
        setBusy(false)
        conversationId = null
        history.length = 0
        save()
        list.replaceChildren()
        intro.hidden = false
    })
}
