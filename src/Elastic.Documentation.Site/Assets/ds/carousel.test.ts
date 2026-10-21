import { initCarousels } from './carousel'

describe('carousel', () => {
    beforeEach(() => {
        document.body.innerHTML = `
<div data-carousel>
  <div data-carousel-track></div>
  <div data-carousel-controls hidden>
    <button data-carousel-prev></button>
    <button data-carousel-dot="0" aria-current="true"></button>
    <button data-carousel-dot="1" aria-current="false"></button>
    <button data-carousel-next></button>
  </div>
</div>`
        const track = document.querySelector<HTMLElement>(
            '[data-carousel-track]'
        )!
        Object.defineProperty(track, 'clientWidth', { value: 100 })
        track.scrollTo = jest.fn() as unknown as typeof track.scrollTo
        initCarousels()
    })

    it('reveals the controls', () => {
        expect(
            document.querySelector<HTMLElement>('[data-carousel-controls]')!
                .hidden
        ).toBe(false)
    })

    it('scrolls to the next slide', () => {
        document.querySelector<HTMLElement>('[data-carousel-next]')!.click()

        expect(
            document.querySelector<HTMLElement>('[data-carousel-track]')!
                .scrollTo
        ).toHaveBeenCalledWith({ left: 100 })
    })

    it('does not scroll before the first slide', () => {
        document.querySelector<HTMLElement>('[data-carousel-prev]')!.click()

        expect(
            document.querySelector<HTMLElement>('[data-carousel-track]')!
                .scrollTo
        ).toHaveBeenCalledWith({ left: 0 })
    })

    it('marks the dot of the visible slide as current', () => {
        const track = document.querySelector<HTMLElement>(
            '[data-carousel-track]'
        )!
        track.scrollLeft = 100
        track.dispatchEvent(new Event('scroll'))

        const dots = document.querySelectorAll('[data-carousel-dot]')
        expect(dots[1].getAttribute('aria-current')).toBe('true')
        expect(dots[0].getAttribute('aria-current')).toBe('false')
    })
})
