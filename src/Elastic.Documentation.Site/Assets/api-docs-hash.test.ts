import { revealCollapsedAncestors } from './api-docs'

function markup(): string {
    return `
        <section id="elastic-api-v3">
            <div class="property-item collapsed" id="outer">
                <dt id="rule">rule</dt>
                <div class="nested-properties" hidden="until-found">
                    <div class="union-variants-container collapsible collapsed" id="options">
                        <div class="union-collapse-toggle">
                            <button class="expand-toggle union-group-toggle" aria-expanded="false"></button>
                        </div>
                        <div class="union-variants union-variants-content" hidden="until-found">
                            <div class="union-variant-item collapsed" id="eql">
                                <div class="nested-properties" hidden="until-found">
                                    <div class="property-item collapsed" id="actions-row">
                                        <dt id="actions">actions</dt>
                                        <div class="nested-properties" hidden="until-found"></div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    `
}

describe('revealCollapsedAncestors', () => {
    beforeEach(() => {
        document.body.innerHTML = markup()
    })

    const byId = (id: string) => document.getElementById(id)!

    it('opens every collapsed list around a link target', () => {
        expect(revealCollapsedAncestors(byId('actions'))).toBe(true)

        expect(byId('eql').classList.contains('expanded')).toBe(true)
        expect(byId('options').classList.contains('expanded')).toBe(true)
        expect(byId('outer').classList.contains('expanded')).toBe(true)
        expect(
            byId('options')
                .querySelector('.union-variants-content')!
                .hasAttribute('hidden')
        ).toBe(false)
    })

    it("leaves the target's own row as it was", () => {
        revealCollapsedAncestors(byId('actions'))

        expect(byId('actions-row').classList.contains('collapsed')).toBe(true)
    })

    it('reports nothing to reveal when the target is already visible', () => {
        byId('outer').classList.remove('collapsed')
        byId('options').classList.remove('collapsed')
        byId('eql').classList.remove('collapsed')

        expect(revealCollapsedAncestors(byId('actions'))).toBe(false)
    })
})
