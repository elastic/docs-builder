import { apiLanguageStorageKey, initApiExamples } from './api-examples-carousel'

// Java is the only long sample: 20 lines, past the 16-line cap.
function source(lang: string): string {
    return lang === 'Java'
        ? Array.from({ length: 20 }, (_, i) => `line ${i + 1}`).join('\n')
        : `${lang} sample`
}

function card(lang: string): string {
    return `
        <section class="api-code-card api-code-carousel-card" data-code-card data-lang="${lang}">
            <header class="api-code-carousel-card-header"><span class="api-code-carousel-card-language">${lang}</span><a class="api-code-carousel-card-client" href="https://example.test/${lang}">${lang} client</a><span data-code-actions></span></header>
            <div><pre><code>${source(lang)}</code></pre></div>
        </section>`
}

function carousel(langs: string[]): string {
    const dots = langs
        .map(
            (lang) =>
                `<button class="api-code-carousel-dot" data-carousel-dot="${lang}" data-tooltip="${lang}"></button>`
        )
        .join('')
    return `
        <div class="api-code-carousel" data-api-carousel>
            <span data-carousel-position></span>
            <button data-carousel-step="-1"></button>
            <button data-carousel-step="1"></button>
            <div data-example-description>
                <div class="api-example-description-text">A description.</div>
                <button data-description-toggle aria-expanded="false" hidden>Show more</button>
            </div>
            <div class="api-code-carousel-strip" data-carousel-strip tabindex="0">${langs.map(card).join('')}</div>
            <div>${dots}</div>
        </div>`
}

const all = ['Console', 'curl', 'Python', 'Java']

function markup(): string {
    return `
        <div data-api-examples>
            <button class="api-example-chip is-active" data-scenario="term"><span class="api-example-chip-title">Term search</span></button>
            <button class="api-example-chip" data-scenario="slicing"><span class="api-example-chip-title">Slicing</span></button>
            <div class="api-examples-scenario-panel" data-scenario="term">${carousel(all)}
                <div class="api-code-card example-block example-block--response" data-code-card>
                    <div class="example-block-header"><span data-code-actions></span></div>
                    <div data-code-panel="200"><pre><code>{"ok":true}</code></pre></div>
                    <div data-code-panel="400" hidden><p class="example-empty">No example</p></div>
                </div>
                <div class="api-code-card" data-code-card data-empty-card>
                    <div class="example-block-header"><span data-code-actions></span></div>
                    <p class="example-empty">No example</p>
                </div>
            </div>
            <div class="api-examples-scenario-panel" data-scenario="slicing" hidden="until-found">${carousel(['Console', 'curl'])}</div>
        </div>`
}

function activeLang(scenario: string): string | undefined {
    return document.querySelector<HTMLElement>(
        `.api-examples-scenario-panel[data-scenario="${scenario}"] .api-code-carousel-card.is-active`
    )?.dataset.lang
}

function click(selector: string) {
    document.querySelector<HTMLElement>(selector)!.click()
}

describe('API examples carousel', () => {
    beforeEach(() => {
        window.localStorage.clear()
        window.sessionStorage.clear()
        window.history.replaceState(null, '', '#')
        document.body.innerHTML = markup()
    })

    it('opens on Console when the reader has not picked a language', () => {
        initApiExamples()

        expect(activeLang('term')).toBe('Console')
        expect(
            document.querySelector('[data-carousel-position] strong')
                ?.textContent
        ).toBe('Console')
    })

    it('never hides language cards, so find-in-page reaches them', () => {
        initApiExamples()

        const hidden = document.querySelectorAll(
            '.api-code-carousel-card[hidden]'
        )
        expect(hidden).toHaveLength(0)
    })

    it('remembers a dot pick across pages in localStorage', () => {
        initApiExamples()
        click('[data-scenario="term"] [data-carousel-dot="Java"]')

        expect(activeLang('term')).toBe('Java')
        expect(window.localStorage.getItem(apiLanguageStorageKey)).toBe('Java')

        document.body.innerHTML = markup()
        initApiExamples()
        expect(activeLang('term')).toBe('Java')
    })

    it('treats a swipe to another card as a pick: link and preference follow', () => {
        type Callback = (entries: Partial<IntersectionObserverEntry>[]) => void
        const callbacks: Callback[] = []
        const Original = window.IntersectionObserver
        window.IntersectionObserver = class {
            constructor(callback: Callback) {
                callbacks.push(callback)
            }
            observe() {}
            disconnect() {}
            unobserve() {}
        } as unknown as typeof IntersectionObserver
        try {
            initApiExamples()
            // Observers register in document order; the first one watches the term carousel.
            const settle = (lang: string) =>
                callbacks[0]([
                    {
                        isIntersecting: true,
                        intersectionRatio: 1,
                        target: document.querySelector(
                            `[data-scenario="term"] .api-code-carousel-card[data-lang="${lang}"]`
                        )!,
                    },
                ])

            // The first report after load names the card that is already active and changes nothing.
            settle('Console')
            expect(window.location.hash).toBe('')
            expect(
                window.localStorage.getItem(apiLanguageStorageKey)
            ).toBeNull()

            settle('Python')

            expect(activeLang('term')).toBe('Python')
            expect(window.location.hash).toBe('#example=term&lang=python')
            expect(window.localStorage.getItem(apiLanguageStorageKey)).toBe(
                'Python'
            )
        } finally {
            window.IntersectionObserver = Original
        }
    })

    it('restores example and language from the deep link', () => {
        window.history.replaceState(null, '', '#example=slicing&lang=curl')

        initApiExamples()

        expect(
            document
                .querySelector(
                    '[data-scenario="slicing"].api-examples-scenario-panel'
                )
                ?.hasAttribute('hidden')
        ).toBe(false)
        expect(activeLang('slicing')).toBe('curl')
    })

    it('writes the link in lowercase and reads it in any case', () => {
        initApiExamples()
        click('[data-scenario="term"] [data-carousel-dot="Java"]')

        expect(window.location.hash).toBe('#example=term&lang=java')

        window.history.replaceState(null, '', '#example=SLICING&lang=Curl')
        document.body.innerHTML = markup()
        initApiExamples()

        expect(activeLang('slicing')).toBe('curl')
    })

    it('keeps every pick smooth, snaps off while gliding, and lands on the target afterwards', () => {
        jest.useFakeTimers()
        initApiExamples()
        const strip = document.querySelector<HTMLElement>(
            '[data-scenario="term"] [data-carousel-strip]'
        )!
        strip
            .querySelectorAll<HTMLElement>('.api-code-carousel-card')
            .forEach((card, i) =>
                Object.defineProperty(card, 'offsetLeft', { value: i * 400 })
            )
        const scrollTo = jest.fn()
        strip.scrollTo = scrollTo as unknown as typeof strip.scrollTo
        const dot = (lang: string) =>
            document.querySelector<HTMLElement>(
                `[data-scenario="term"] [data-carousel-dot="${lang}"]`
            )!

        dot('Python').click()
        dot('Java').click()

        expect(scrollTo.mock.calls.map((c) => c[0].behavior)).toEqual([
            'smooth',
            'smooth',
        ])
        expect(strip.style.scrollSnapType).toBe('none')
        expect(activeLang('term')).toBe('Java')

        // The browser stopped on Python (index 2) instead of reaching Java (index 3).
        Object.defineProperty(strip, 'scrollLeft', {
            value: 800,
            configurable: true,
        })
        jest.advanceTimersByTime(900)

        expect(strip.style.scrollSnapType).toBe('')
        expect(scrollTo.mock.calls[scrollTo.mock.calls.length - 1][0]).toEqual({
            left: 1200,
            behavior: 'auto',
        })
        expect(activeLang('term')).toBe('Java')
        jest.useRealTimers()
    })

    it('gives the dots a fast tooltip instead of a native title', () => {
        initApiExamples()
        const dot = document.querySelector<
            HTMLElement & {
                _tippy?: { props: { content: string; delay: unknown } }
            }
        >('[data-scenario="term"] [data-carousel-dot="Java"]')!

        expect(dot.hasAttribute('title')).toBe(false)
        expect(dot._tippy?.props.content).toBe('Java')
        expect(dot._tippy?.props.delay).toEqual([80, 0])
    })

    it('steps with the arrows and disables them at the ends', () => {
        initApiExamples()
        const prev = document.querySelector<HTMLButtonElement>(
            '[data-scenario="term"] [data-carousel-step="-1"]'
        )!
        expect(prev.disabled).toBe(true)

        click('[data-scenario="term"] [data-carousel-step="1"]')

        expect(activeLang('term')).toBe('curl')
        expect(prev.disabled).toBe(false)
    })

    it('switches to the dimmed neighbour card when it is clicked', () => {
        initApiExamples()

        click(
            '[data-scenario="term"] .api-code-carousel-card[data-lang="curl"] pre'
        )

        expect(activeLang('term')).toBe('curl')
        expect(window.localStorage.getItem(apiLanguageStorageKey)).toBe('curl')
    })

    it('switches example and keeps the language when the example has it', () => {
        initApiExamples()
        click('[data-scenario="term"] [data-carousel-dot="curl"]')

        click('.api-example-chip[data-scenario="slicing"]')

        const slicing = document.querySelector(
            '.api-examples-scenario-panel[data-scenario="slicing"]'
        )
        const term = document.querySelector(
            '.api-examples-scenario-panel[data-scenario="term"]'
        )
        expect(slicing?.hasAttribute('hidden')).toBe(false)
        expect(term?.getAttribute('hidden')).toBe('until-found')
        expect(activeLang('slicing')).toBe('curl')
    })

    it('offers only the languages an example has', () => {
        initApiExamples()
        click('.api-example-chip[data-scenario="slicing"]')

        const dots = [
            ...document.querySelectorAll<HTMLElement>(
                '[data-scenario="slicing"] .api-code-carousel-dot'
            ),
        ].map((dot) => dot.dataset.carouselDot)

        expect(dots).toEqual(['Console', 'curl'])
    })

    it('shows a scenario when find-in-page matches inside it', () => {
        initApiExamples()
        const slicing = document.querySelector<HTMLElement>(
            '.api-examples-scenario-panel[data-scenario="slicing"]'
        )!

        slicing.dispatchEvent(new Event('beforematch'))

        expect(slicing.hasAttribute('hidden')).toBe(false)
    })

    it('offers the preview only on cards that have code', () => {
        initApiExamples()

        expect(
            document.querySelector(
                '.example-block--response [data-code-preview]'
            )
        ).not.toBeNull()
        expect(
            document.querySelector('[data-empty-card] [data-code-preview]')
        ).toBeNull()
    })

    it('copies from the preview, and does nothing without a Clipboard API', async () => {
        initApiExamples()
        const open = () => {
            document
                .querySelector<HTMLElement>(
                    '[data-scenario="term"] .is-active [data-code-preview]'
                )!
                .click()
            return document.querySelector<HTMLElement>(
                'dialog.api-code-preview [aria-label="Copy code"]'
            )!
        }

        Object.defineProperty(navigator, 'clipboard', {
            value: undefined,
            configurable: true,
        })
        expect(() => open().click()).not.toThrow()
        document
            .querySelector('dialog.api-code-preview')!
            .dispatchEvent(new Event('close'))

        const writeText = jest.fn(() => Promise.resolve())
        Object.defineProperty(navigator, 'clipboard', {
            value: { writeText },
            configurable: true,
        })
        open().click()
        await Promise.resolve()

        expect(writeText).toHaveBeenCalledWith('Console sample')
        document
            .querySelector('dialog.api-code-preview')!
            .dispatchEvent(new Event('close'))
    })

    it('locks page scrolling while the preview is open', () => {
        initApiExamples()
        document
            .querySelector<HTMLElement>(
                '[data-scenario="term"] .is-active [data-code-preview]'
            )!
            .click()
        const dialog = document.querySelector<HTMLDialogElement>(
            'dialog.api-code-preview'
        )!

        expect(document.documentElement.style.overflow).toBe('hidden')

        dialog.dispatchEvent(new Event('close'))

        expect(document.documentElement.style.overflow).toBe('')
        expect(document.querySelector('dialog.api-code-preview')).toBeNull()
    })

    it('closes the preview on Escape even when the page prevents the key', () => {
        initApiExamples()
        document.addEventListener('keydown', (e) => e.preventDefault())
        document
            .querySelector<HTMLElement>(
                '[data-scenario="term"] .is-active [data-code-preview]'
            )!
            .click()
        const dialog = document.querySelector<HTMLDialogElement>(
            'dialog.api-code-preview'
        )!
        dialog.close = jest.fn(() => dialog.remove())

        dialog
            .querySelector('button')!
            .dispatchEvent(
                new KeyboardEvent('keydown', { key: 'Escape', bubbles: true })
            )

        expect(dialog.close).toHaveBeenCalled()
    })

    it('previews the code of a card in a dialog', () => {
        initApiExamples()
        const button = document.querySelector<HTMLElement>(
            '[data-scenario="term"] .is-active [data-code-preview]'
        )!

        button.click()

        const dialog = document.querySelector<HTMLDialogElement>(
            'dialog.api-code-preview'
        )!
        expect(
            dialog.querySelector('.api-code-carousel-card-language')
                ?.textContent
        ).toBe('Console')
        expect(dialog.querySelector('pre code')?.textContent).toBe(
            'Console sample'
        )
        expect(
            dialog.querySelector<HTMLAnchorElement>(
                'a.api-code-carousel-card-client'
            )?.href
        ).toBe('https://example.test/Console')
        dialog.close = jest.fn(() => dialog.remove())
        dialog.querySelector<HTMLElement>('[aria-label="Close"]')!.click()
        expect(dialog.close).toHaveBeenCalled()
    })

    it('opens and closes a long description', () => {
        initApiExamples()
        const toggle = document.querySelector<HTMLElement>(
            '[data-scenario="term"] [data-description-toggle]'
        )!
        const description = toggle.closest('[data-example-description]')!

        toggle.click()

        expect(description.classList.contains('is-open')).toBe(true)
        expect(toggle.textContent).toBe('Show less')
        expect(toggle.getAttribute('aria-expanded')).toBe('true')

        toggle.click()

        expect(description.classList.contains('is-open')).toBe(false)
        expect(toggle.textContent).toBe('Show more')
    })
})
