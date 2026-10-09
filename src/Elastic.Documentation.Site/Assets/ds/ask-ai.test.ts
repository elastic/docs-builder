import {
    initAskAi,
    rateLimitMessage,
    renderAnswer,
    splitReferences,
    streamAnswer,
    type AskAiStream,
} from './ask-ai'

jest.mock('../config', () => ({
    config: {
        buildType: 'assembler',
        apiBasePath: '/docs/_api',
        rootPath: '/docs',
    },
}))

const markup = `
<button data-ask-ai-open="a" hidden>Ask AI</button>
<dialog id="a" data-ask-ai>
  <button data-ask-ai-new></button>
  <div data-ask-ai-intro><p data-ask-ai-description></p><ul data-ask-ai-suggestions></ul></div>
  <ol data-ask-ai-messages></ol>
  <form data-ask-ai-form>
    <textarea data-ask-ai-input></textarea>
    <button type="submit" data-ask-ai-send>Send</button>
    <button type="button" data-ask-ai-stop hidden>Stop</button>
  </form>
  <template data-ask-ai-message><li><p class="ask-ai__author"></p><div class="ask-ai__text"></div></li></template>
</dialog>`

const flush = () => new Promise((r) => setTimeout(r, 0))

function setup(stream: AskAiStream) {
    document.body.innerHTML = markup
    const dialog = document.getElementById('a') as HTMLDialogElement
    dialog.showModal = jest.fn()
    dialog.close = jest.fn()
    initAskAi(document, stream)
    return dialog
}

const submit = (text: string) => {
    const input = document.querySelector<HTMLTextAreaElement>(
        '[data-ask-ai-input]'
    )!
    input.value = text
    document
        .querySelector<HTMLFormElement>('[data-ask-ai-form]')!
        .dispatchEvent(new Event('submit', { cancelable: true }))
}

describe('ask ai', () => {
    beforeEach(() => sessionStorage.clear())

    it('reveals the trigger and opens the panel', () => {
        const dialog = setup(async () => undefined)
        const trigger =
            document.querySelector<HTMLButtonElement>('[data-ask-ai-open]')!

        expect(trigger.hidden).toBe(false)
        trigger.click()
        expect(dialog.showModal).toHaveBeenCalled()
    })

    it('offers suggestions that ask the question', async () => {
        const seen: string[] = []
        setup(async (request) => void seen.push(request.message))

        expect(
            document.querySelectorAll('.ask-ai__suggestion').length
        ).toBeGreaterThan(0)
        document
            .querySelector<HTMLButtonElement>('.ask-ai__suggestion')!
            .click()
        await flush()

        expect(seen).toHaveLength(1)
    })

    it('streams an answer, keeps the conversation id, and hides the intro', async () => {
        const requests: Array<{
            message: string
            conversationId: string | null
        }> = []
        setup(async (request) => {
            requests.push({
                message: request.message,
                conversationId: request.conversationId,
            })
            request.onEvent({
                type: 'conversation_start',
                conversationId: 'c1',
            })
            request.onEvent({
                type: 'message_chunk',
                content: 'Use **a data stream**',
            })
            request.onEvent({
                type: 'message_complete',
                fullContent: 'Use **a data stream**.',
            })
        })

        submit('How do I index logs?')
        await flush()
        submit('And metrics?')
        await flush()

        const messages = document.querySelectorAll('[data-ask-ai-messages] li')
        expect(messages).toHaveLength(4)
        expect(messages[1].querySelector('strong')!.textContent).toBe(
            'a data stream'
        )
        expect(requests[1].conversationId).toBe('c1')
        expect(
            document.querySelector<HTMLElement>('[data-ask-ai-intro]')!.hidden
        ).toBe(true)
    })

    it('shows an error from the stream', async () => {
        setup(async (request) =>
            request.onEvent({ type: 'error', message: 'Rate limited' })
        )

        submit('hello')
        await flush()

        const answer = document.querySelector<HTMLElement>(
            '[data-role="assistant"]'
        )!
        expect(answer.dataset.state).toBe('error')
        expect(answer.textContent).toContain('Rate limited')
    })

    it('starts a new chat', async () => {
        setup(async (request) =>
            request.onEvent({ type: 'message_complete', fullContent: 'ok' })
        )
        submit('hello')
        await flush()

        document.querySelector<HTMLButtonElement>('[data-ask-ai-new]')!.click()

        expect(
            document.querySelectorAll('[data-ask-ai-messages] li')
        ).toHaveLength(0)
        expect(
            document.querySelector<HTMLElement>('[data-ask-ai-intro]')!.hidden
        ).toBe(false)
    })

    it('sanitizes the rendered answer', () => {
        expect(renderAnswer('<img src=x onerror=alert(1)>hi')).not.toContain(
            'onerror'
        )
    })
})

describe('rate limits', () => {
    it('says how long to wait when the server says', () => {
        expect(rateLimitMessage('30')).toContain('30 seconds')
        expect(rateLimitMessage(null)).toContain('in a moment')
    })
})

describe('single answers', () => {
    it('reports text as it arrives and failures once', async () => {
        const texts: string[] = []
        const failures: string[] = []
        await streamAnswer(
            'q',
            new AbortController().signal,
            (text) => texts.push(text),
            (message) => failures.push(message),
            async (request) => {
                request.onEvent({ type: 'message_chunk', content: 'a' })
                request.onEvent({ type: 'message_chunk', content: 'b' })
                request.onEvent({ type: 'error', message: 'nope' })
            }
        )

        expect(texts).toEqual(['a', 'ab'])
        expect(failures).toEqual(['nope'])
    })
})

describe('conversation history', () => {
    beforeEach(() => sessionStorage.clear())

    it('remembers the conversation across pages and clears it on a new chat', async () => {
        setup(async (request) => {
            request.onEvent({
                type: 'conversation_start',
                conversationId: 'c9',
            })
            request.onEvent({
                type: 'message_complete',
                fullContent: 'Use **a data stream**.',
            })
        })
        submit('How do I index logs?')
        await flush()

        // A new page brings a new dialog; it restores what was said.
        setup(async () => undefined)

        const messages = document.querySelectorAll('[data-ask-ai-messages] li')
        expect(messages).toHaveLength(2)
        expect(messages[1].querySelector('strong')!.textContent).toBe(
            'a data stream'
        )
        expect(
            document.querySelector<HTMLElement>('[data-ask-ai-intro]')!.hidden
        ).toBe(true)

        document.querySelector<HTMLButtonElement>('[data-ask-ai-new]')!.click()
        expect(sessionStorage.getItem('ask-ai-conversation')).toBeNull()
    })
})

describe('references and feedback', () => {
    beforeEach(() => sessionStorage.clear())

    it('splits references from the answer and hides an unfinished block', () => {
        const done = splitReferences(
            'Answer\n<!--REFERENCES [{"url":"/a","title":"A","description":"d"}] -->'
        )
        expect(done.main).toBe('Answer')
        expect(done.references).toEqual([
            { url: '/a', title: 'A', description: 'd' },
        ])

        expect(splitReferences('Answer <!--REFERENCES [{"url"')).toEqual({
            main: 'Answer',
            references: [],
        })
        expect(
            splitReferences('Answer <!--REFERENCES nope -->').references
        ).toEqual([])
    })

    it('lists references as links and sends a reaction once', async () => {
        global.fetch = jest
            .fn()
            .mockResolvedValue({ ok: true }) as unknown as typeof fetch
        setup(async (request) => {
            request.onEvent({
                type: 'conversation_start',
                conversationId: 'c1',
            })
            request.onEvent({
                type: 'message_complete',
                fullContent:
                    'Use a data stream.\n<!--REFERENCES [{"url":"/ds","title":"Data streams","description":""}] -->',
            })
        })
        submit('How do I index logs?')
        await flush()

        expect(
            document
                .querySelector('.ask-ai__references a')!
                .getAttribute('href')
        ).toBe('/ds')
        const [up] = Array.from(
            document.querySelectorAll<HTMLButtonElement>('.ask-ai__reaction')
        )
        up.click()
        up.click()

        expect(up.getAttribute('aria-pressed')).toBe('true')
        expect(global.fetch).toHaveBeenCalledTimes(1)
        expect(
            JSON.parse((global.fetch as jest.Mock).mock.calls[0][1].body)
                .reaction
        ).toBe('thumbsUp')
    })
})
