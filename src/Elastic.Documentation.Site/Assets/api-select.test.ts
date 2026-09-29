import { initApiCodeLanguageSelects, initApiScenarioSelects } from './api-docs'

function languageMarkup(): string {
    return `
        <div data-api-code-sample>
            <details class="api-select api-code-sample-lang">
                <summary class="api-select-trigger">
                    <span class="api-select-value">Console</span>
                </summary>
                <div class="api-page-actions-menu" role="listbox">
                    <button type="button" class="api-page-actions-option is-selected" role="option" aria-selected="true" data-value="Console">Console</button>
                    <button type="button" class="api-page-actions-option" role="option" aria-selected="false" data-value="Python">Python</button>
                </div>
            </details>
            <div class="api-code-sample-panel" data-lang="Console"></div>
            <div class="api-code-sample-panel" data-lang="Python" hidden></div>
        </div>
        <div data-api-code-sample>
            <details class="api-select api-code-sample-lang">
                <summary class="api-select-trigger">
                    <span class="api-select-value">Console</span>
                </summary>
                <div class="api-page-actions-menu" role="listbox">
                    <button type="button" class="api-page-actions-option is-selected" role="option" aria-selected="true" data-value="Console">Console</button>
                    <button type="button" class="api-page-actions-option" role="option" aria-selected="false" data-value="Python">Python</button>
                </div>
            </details>
            <div class="api-code-sample-panel" data-lang="Console"></div>
            <div class="api-code-sample-panel" data-lang="Python" hidden></div>
        </div>
    `
}

function scenarioMarkup(): string {
    return `
        <div data-api-scenarios>
            <details class="api-select api-scenario-select" open>
                <summary class="api-select-trigger">
                    <span class="api-select-value">Match all</span>
                </summary>
                <div class="api-page-actions-menu" role="listbox">
                    <button type="button" class="api-page-actions-option is-selected" role="option" aria-selected="true" data-value="match-all">Match all</button>
                    <button type="button" class="api-page-actions-option" role="option" aria-selected="false" data-value="query-string">Query string</button>
                </div>
            </details>
            <div class="api-examples-scenario-panel" data-scenario="match-all"></div>
            <div class="api-examples-scenario-panel" data-scenario="query-string" hidden></div>
        </div>
    `
}

describe('API custom selects', () => {
    afterEach(() => {
        document.body.innerHTML = ''
        window.sessionStorage.clear()
    })

    it('syncs language across sample widgets and persists the choice', () => {
        document.body.innerHTML = languageMarkup()
        initApiCodeLanguageSelects()

        document
            .querySelector<HTMLButtonElement>('[data-value="Python"]')!
            .click()

        const values = Array.from(
            document.querySelectorAll('.api-select-value')
        ).map((node) => node.textContent?.trim())
        expect(values).toEqual(['Python', 'Python'])
        expect(
            document
                .querySelector('[data-lang="Python"]')
                ?.hasAttribute('hidden')
        ).toBe(false)
        expect(
            document
                .querySelector('[data-lang="Console"]')
                ?.hasAttribute('hidden')
        ).toBe(true)
        expect(window.sessionStorage.getItem('tab-id-api-language')).toBe(
            'Python'
        )
    })

    it('keeps a JSON-only sample visible when another language is selected', () => {
        document.body.innerHTML = `
            ${languageMarkup()}
            <div data-api-code-sample>
                <div class="api-code-sample-panel" data-lang="JSON"></div>
            </div>
        `
        initApiCodeLanguageSelects()

        document
            .querySelector<HTMLButtonElement>('[data-value="Python"]')!
            .click()

        const jsonPanel = document.querySelector('[data-lang="JSON"]')
        expect(jsonPanel?.hasAttribute('hidden')).toBe(false)
    })

    it('switches the example scenario and closes the dropdown', () => {
        document.body.innerHTML = scenarioMarkup()
        initApiScenarioSelects()

        const dropdown = document.querySelector<HTMLDetailsElement>(
            '.api-scenario-select'
        )!
        expect(dropdown.open).toBe(true)

        document
            .querySelector<HTMLButtonElement>('[data-value="query-string"]')!
            .click()

        expect(dropdown.open).toBe(false)
        expect(
            document.querySelector('.api-select-value')?.textContent?.trim()
        ).toBe('Query string')
        expect(
            document
                .querySelector('[data-scenario="query-string"]')
                ?.hasAttribute('hidden')
        ).toBe(false)
        expect(
            document
                .querySelector('[data-scenario="match-all"]')
                ?.hasAttribute('hidden')
        ).toBe(true)
    })
})
