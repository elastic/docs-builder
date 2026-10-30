import { updateSectionLinks } from './api-docs'
import { beforeEach, describe, expect, it } from 'vitest'

const markup = `
    <nav data-api-section-links>
        <a href="#path-params" data-section-link="path-params">Path</a>
        <a href="#query-params" data-section-link="query-params">Query</a>
        <a href="#responses" data-section-link="responses">Response</a>
    </nav>
    <section id="path-params"></section>
    <section id="query-params"></section>
    <section id="responses"></section>`

function placeSections(tops: Record<string, number>): void {
    for (const [id, top] of Object.entries(tops))
        document.getElementById(id)!.getBoundingClientRect = () =>
            ({ top, bottom: top, height: 0 }) as DOMRect
}

const current = () =>
    Array.from(document.querySelectorAll('[data-section-link]'))
        .filter((a) => a.hasAttribute('aria-current'))
        .map((a) => (a as HTMLElement).dataset.sectionLink)

describe('section links', () => {
    beforeEach(() => {
        document.body.innerHTML = markup
        // jsdom has no layout: a page taller than the window, scrolled to the top
        Object.defineProperty(document.body, 'scrollHeight', {
            configurable: true,
            value: 5000,
        })
    })

    it('marks the first link while no section has reached the bar', () => {
        placeSections({
            'path-params': 400,
            'query-params': 900,
            responses: 1500,
        })
        updateSectionLinks()
        expect(current()).toEqual(['path-params'])
    })

    it('marks the last section that has passed the bar', () => {
        placeSections({
            'path-params': -600,
            'query-params': 10,
            responses: 700,
        })
        updateSectionLinks()
        expect(current()).toEqual(['query-params'])
    })

    it('marks the last link at the bottom of the page', () => {
        placeSections({
            'path-params': -600,
            'query-params': -300,
            responses: 600,
        })
        Object.defineProperty(document.body, 'scrollHeight', {
            configurable: true,
            value: window.innerHeight,
        })
        updateSectionLinks()
        expect(current()).toEqual(['responses'])
    })

    it('does nothing on a page without the bar', () => {
        document.body.innerHTML = '<p>no bar</p>'
        expect(() => updateSectionLinks()).not.toThrow()
    })
})
