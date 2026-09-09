import { handlePrivacyConsentClick, iubendaActionFor } from './privacy-consent'

function clickOn(html: string): Event {
    document.body.innerHTML = html
    const el = document.body.querySelector('a, button, svg')
    if (!el) throw new Error('missing fixture element')
    return { target: el, preventDefault: jest.fn() } as unknown as Event
}

describe('iubendaActionFor', () => {
    afterEach(() => {
        document.body.innerHTML = ''
    })

    it('maps the preferences link, including icon clicks', () => {
        const event = clickOn(
            '<a class="iubenda-cs-preferences-link" href="#"><svg></svg>Your Privacy Choices</a>'
        )
        expect(iubendaActionFor(event.target)).toBe('preferences')
    })

    it('maps the notice-at-collection link', () => {
        const event = clickOn(
            '<a class="iubenda-cs-uspr-link" href="#">Notice at Collection</a>'
        )
        expect(iubendaActionFor(event.target)).toBe('uspr')
    })

    it('ignores other links', () => {
        const event = clickOn(
            '<a href="https://www.elastic.co/legal/privacy-statement">Privacy</a>'
        )
        expect(iubendaActionFor(event.target)).toBeNull()
    })
})

describe('handlePrivacyConsentClick', () => {
    afterEach(() => {
        document.body.innerHTML = ''
        delete window._iub
    })

    it('opens preferences and stops the hash navigation', () => {
        const openPreferences = jest.fn()
        window._iub = { cs: { api: { openPreferences } } }
        const event = clickOn(
            '<a class="iubenda-cs-preferences-link" href="#">Your Privacy Choices</a>'
        )

        handlePrivacyConsentClick(event)

        expect(event.preventDefault).toHaveBeenCalled()
        expect(openPreferences).toHaveBeenCalled()
    })

    it('does nothing when Iubenda has not loaded', () => {
        const event = clickOn(
            '<a class="iubenda-cs-preferences-link" href="#">Your Privacy Choices</a>'
        )

        handlePrivacyConsentClick(event)

        expect(event.preventDefault).toHaveBeenCalled()
    })
})
