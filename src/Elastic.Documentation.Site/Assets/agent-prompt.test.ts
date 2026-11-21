import {
    copyPrompt,
    initAgentPrompt,
    initAgentPromptImpressions,
    openPrompt,
} from './agent-prompt'
import { logInfo } from './telemetry/logging'

vi.mock('./telemetry/logging', () => ({ logInfo: vi.fn() }))

const PROMPT = 'Follow the quickstart.\n1. Ask me for my URL & API key.'

function mountPage() {
    document.body.innerHTML = `
        <div class="agent-prompt">
            <p class="agent-prompt-title">Run this quickstart</p>
            <p class="agent-prompt-description">Copy a prompt.</p>
            <button class="agent-prompt-open">Review prompt</button>
            <button class="agent-prompt-button agent-prompt-copy" id="inline-copy">Copy prompt</button>
            <dialog class="agent-prompt-dialog">
                <button class="agent-prompt-close">x</button>
                <pre class="agent-prompt-body"><code>${PROMPT}</code></pre>
                <button class="agent-prompt-button agent-prompt-copy" id="dialog-copy">Copy prompt</button>
            </dialog>
        </div>`
}

const click = (selector: string) =>
    document.querySelector<HTMLElement>(selector)!.click()

const events = () => vi.mocked(logInfo).mock.calls.map((call) => call[0])

describe('agent prompt events', () => {
    let writeText: ReturnType<typeof vi.fn>

    beforeAll(() => {
        // jsdom has no modal dialog support.
        HTMLDialogElement.prototype.showModal = function () {
            this.setAttribute('open', '')
        }
        HTMLDialogElement.prototype.close = function () {
            this.removeAttribute('open')
        }
    })

    beforeEach(() => {
        vi.mocked(logInfo).mockClear()
        writeText = vi.fn().mockResolvedValue(undefined)
        Object.defineProperty(navigator, 'clipboard', {
            value: { writeText },
            configurable: true,
        })
        mountPage()
    })

    it('logs agent_prompt_copied after a successful copy from the inline card', async () => {
        initAgentPrompt()
        click('#inline-copy')

        await vi.waitFor(() => expect(logInfo).toHaveBeenCalledOnce())
        expect(writeText).toHaveBeenCalledWith(PROMPT)
        expect(logInfo).toHaveBeenCalledWith(
            'agent_prompt_copied',
            expect.objectContaining({
                'event.name': 'agent_prompt_copied',
                'agent_prompt.location': 'inline',
                'agent_prompt.title': 'Run this quickstart',
                'agent_prompt.length': PROMPT.length,
                'agent_prompt.index': 0,
            })
        )
    })

    it('logs the dialog location when copying from inside the dialog', async () => {
        initAgentPrompt()
        click('#dialog-copy')

        await vi.waitFor(() => expect(logInfo).toHaveBeenCalledOnce())
        expect(logInfo).toHaveBeenCalledWith(
            'agent_prompt_copied',
            expect.objectContaining({ 'agent_prompt.location': 'dialog' })
        )
    })

    it('logs agent_prompt_copy_failed, and not agent_prompt_copied, when the clipboard write fails', async () => {
        writeText.mockRejectedValue(
            new DOMException('denied', 'NotAllowedError')
        )
        vi.spyOn(console, 'error').mockImplementation(() => {})
        initAgentPrompt()
        click('#inline-copy')

        await vi.waitFor(() => expect(logInfo).toHaveBeenCalledOnce())
        expect(logInfo).toHaveBeenCalledWith(
            'agent_prompt_copy_failed',
            expect.objectContaining({ 'error.type': 'NotAllowedError' })
        )
        expect(events()).not.toContain('agent_prompt_copied')
    })

    it('shows "Copied!" only when the copy succeeded', async () => {
        initAgentPrompt()
        click('#inline-copy')

        await vi.waitFor(() =>
            expect(document.querySelector('#inline-copy')?.textContent).toBe(
                'Copied!'
            )
        )
    })

    it('logs agent_prompt_review_opened once, even if opened again while open', () => {
        initAgentPrompt()
        click('.agent-prompt-open')
        click('.agent-prompt-open')

        expect(events()).toEqual(['agent_prompt_review_opened'])
        expect(logInfo).toHaveBeenCalledWith(
            'agent_prompt_review_opened',
            expect.objectContaining({ 'agent_prompt.location': 'inline' })
        )
    })

    it('exposes copyPrompt and openPrompt for other callers', async () => {
        initAgentPrompt()
        const root = document.querySelector<HTMLElement>('.agent-prompt')!

        expect(await copyPrompt(root, 'dialog')).toBe(true)
        openPrompt(root, 'inline')
        expect(events()).toEqual([
            'agent_prompt_copied',
            'agent_prompt_review_opened',
        ])
    })

    it('never sends the prompt text in an event', async () => {
        initAgentPrompt()
        click('#inline-copy')
        click('.agent-prompt-open')
        await vi.waitFor(() => expect(logInfo).toHaveBeenCalledTimes(2))

        for (const call of vi.mocked(logInfo).mock.calls)
            expect(JSON.stringify(call)).not.toContain('Follow the quickstart')
    })

    describe('impressions', () => {
        let callback: IntersectionObserverCallback
        const unobserve = vi.fn()

        // The shared setup unstubs globals after each test, so stub before each one.
        beforeEach(() => {
            unobserve.mockClear()
            vi.stubGlobal(
                'IntersectionObserver',
                class {
                    constructor(cb: IntersectionObserverCallback) {
                        callback = cb
                    }
                    observe() {}
                    unobserve = unobserve
                    disconnect() {}
                }
            )
        })

        const entry = (selector: string, isIntersecting: boolean) =>
            ({
                target: document.querySelector(selector),
                isIntersecting,
            }) as unknown as IntersectionObserverEntry

        it('logs agent_prompt_viewed once when the card is visible, and ignores it while hidden', () => {
            initAgentPrompt()
            initAgentPromptImpressions()
            const observer = { unobserve } as unknown as IntersectionObserver

            callback([entry('.agent-prompt', false)], observer)
            expect(logInfo).not.toHaveBeenCalled()

            callback([entry('.agent-prompt', true)], observer)
            expect(logInfo).toHaveBeenCalledOnce()
            expect(logInfo).toHaveBeenCalledWith(
                'agent_prompt_viewed',
                expect.objectContaining({ 'agent_prompt.location': 'inline' })
            )
            expect(unobserve).toHaveBeenCalledOnce()
        })
    })
})
