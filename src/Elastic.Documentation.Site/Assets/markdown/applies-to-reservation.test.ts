import { readFileSync } from 'node:fs'
import { join } from 'node:path'

const css = readFileSync(join(__dirname, 'applies-to.css'), 'utf8')

describe('applies-block pre-hydration reservation', () => {
    it('sizes empty hosts from badge attributes so flex-wrap can match hydration', () => {
        expect(css).toContain('applies-to-popover:not(:defined)')
        expect(css).toContain('applies-to-popover:empty')
        expect(css).toContain('attr(badge-key)')
        expect(css).not.toMatch(/min-height:\s*calc\(\s*1\.875rem/)
    })
})
