import { plainText } from './AIAnswerPanel'
import { describe, expect, it } from 'vitest'

describe('plainText', () => {
    it('reads the text of a highlighted result title', () => {
        expect(
            plainText('Search &amp; <mark>analytics</mark> &lt;tenant&gt;')
        ).toBe('Search & analytics <tenant>')
    })
})
