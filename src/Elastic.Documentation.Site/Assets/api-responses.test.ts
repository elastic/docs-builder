import { initApiDocs } from './api-docs'

function responseMarkup(): string {
    return `
        <section id="elastic-api-v3">
            <div class="response-panel collapsed" data-status="200">
                <div class="response-summary">
                    <div class="response-status-row">
                        <button
                            type="button"
                            class="response-status-toggle"
                            aria-expanded="false"
                            aria-controls="response-200-fields">
                            <span class="response-status-chip status-success">200</span>
                            <span class="content-type-tag">application/json</span>
                            <span class="response-toggle-icon" aria-hidden="true"></span>
                        </button>
                    </div>
                    <div class="response-description">
                        See the <a href="https://www.elastic.co/guide">guide</a>.
                    </div>
                </div>
                <div class="response-panel-body" id="response-200-fields" hidden="until-found"></div>
            </div>
        </section>
    `
}

describe('response status row', () => {
    beforeAll(() => initApiDocs())

    beforeEach(() => {
        document.body.innerHTML = responseMarkup()
    })

    const panel = () => document.querySelector<HTMLElement>('.response-panel')!
    const toggle = () =>
        document.querySelector<HTMLButtonElement>('.response-status-toggle')!

    it('expands when the media type in the header is clicked', () => {
        document.querySelector<HTMLElement>('.content-type-tag')!.click()

        expect(panel().classList.contains('expanded')).toBe(true)
        expect(toggle().getAttribute('aria-expanded')).toBe('true')
    })

    it('leaves the panel collapsed when a description link is clicked', () => {
        const link = document.querySelector('a')!
        const event = new MouseEvent('click', {
            bubbles: true,
            cancelable: true,
        })
        link.dispatchEvent(event)

        expect(event.defaultPrevented).toBe(false)
        expect(panel().classList.contains('collapsed')).toBe(true)
        expect(toggle().getAttribute('aria-expanded')).toBe('false')
    })

    it('leaves the panel collapsed when text is selected', () => {
        window
            .getSelection()
            ?.selectAllChildren(
                document.querySelector('.response-description')!
            )

        toggle().click()
        window.getSelection()?.removeAllRanges()

        expect(panel().classList.contains('collapsed')).toBe(true)
        expect(toggle().getAttribute('aria-expanded')).toBe('false')
    })
})
