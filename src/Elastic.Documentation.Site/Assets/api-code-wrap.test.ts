import { apiCodeWrapStorageKey, initApiCodeWrap } from './api-code-wrap'
import { beforeEach, describe, expect, it } from 'vitest'

const card = `<section data-code-card><span data-code-actions></span><pre><code>x</code></pre></section>`

describe('initApiCodeWrap', () => {
    beforeEach(() => {
        window.localStorage.clear()
        document.documentElement.removeAttribute('data-api-code-wrap')
        document.body.innerHTML = `<aside id="api-examples-panel">${card}</aside><div>${card}</div>`
    })

    it('adds the toggle to examples cards only, once', () => {
        initApiCodeWrap()
        initApiCodeWrap()
        expect(
            document.querySelectorAll('[data-code-wrap-toggle]')
        ).toHaveLength(1)
        expect(
            document.querySelector(
                '#api-examples-panel [data-code-wrap-toggle]'
            )
        ).not.toBeNull()
    })

    it('toggles wrapping and remembers the choice', () => {
        initApiCodeWrap()
        const button = document.querySelector<HTMLButtonElement>(
            '[data-code-wrap-toggle]'
        )!
        button.click()
        expect(
            document.documentElement.hasAttribute('data-api-code-wrap')
        ).toBe(true)
        expect(button.getAttribute('aria-pressed')).toBe('true')
        expect(window.localStorage.getItem(apiCodeWrapStorageKey)).toBe('true')
        button.click()
        expect(
            document.documentElement.hasAttribute('data-api-code-wrap')
        ).toBe(false)
    })

    it('restores a remembered choice', () => {
        window.localStorage.setItem(apiCodeWrapStorageKey, 'true')
        initApiCodeWrap()
        expect(
            document.documentElement.hasAttribute('data-api-code-wrap')
        ).toBe(true)
    })
})
