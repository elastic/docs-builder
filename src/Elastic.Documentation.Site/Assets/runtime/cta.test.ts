import { logInfo } from '../telemetry/logging'
import { bindCtaClicks } from './cta'

vi.mock('../telemetry/logging', () => ({ logInfo: vi.fn() }))

describe('bindCtaClicks', () => {
    beforeAll(() => bindCtaClicks())

    beforeEach(() => {
        vi.mocked(logInfo).mockClear()
        document.body.innerHTML = `
            <a id="cta" data-cta="try" data-cta-url="/try" data-cta-label="Try it" data-cta-location="hero"><span id="inner">Try</span></a>
            <a id="plain">Plain</a>`
    })

    const fire = (id: string, type: string, init: MouseEventInit = {}) =>
        document
            .getElementById(id)!
            .dispatchEvent(new MouseEvent(type, { bubbles: true, ...init }))

    it('logs cta_clicked with the CTA attributes, also from a child element', () => {
        fire('inner', 'click')

        expect(logInfo).toHaveBeenCalledWith(
            'cta_clicked',
            expect.objectContaining({ 'event.name': 'cta_clicked' })
        )
    })

    it('logs a middle-click, which does not fire click', () => {
        fire('cta', 'auxclick', { button: 1 })

        expect(logInfo).toHaveBeenCalledOnce()
    })

    it('ignores a right-click and links that are not CTAs', () => {
        fire('cta', 'auxclick', { button: 2 })
        fire('plain', 'click')

        expect(logInfo).not.toHaveBeenCalled()
    })
})
