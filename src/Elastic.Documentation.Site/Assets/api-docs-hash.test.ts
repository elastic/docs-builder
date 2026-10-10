import { revealCollapsedAncestors } from './api-docs'

function markup(): string {
    return `
        <section id="elastic-api-v3">
            <div class="property-item collapsed" id="outer">
                <dt id="rule">rule</dt>
                <div class="nested-properties" hidden="until-found">
                    <div class="union-variants-container union-variant-chips" id="options">
                        <div class="api-example-chips-row" data-chip-row>
                            <div class="api-example-chips" role="tablist">
                                <button class="api-example-chip is-active" data-chip="query" aria-selected="true"><span data-chip-title>query</span></button>
                                <button class="api-example-chip" data-chip="eql" aria-selected="false"><span data-chip-title>eql</span></button>
                                <button class="api-example-chip" data-chip="esql" aria-selected="false"><span data-chip-title>esql</span></button>
                            </div>
                        </div>
                        <div class="union-variants">
                            <div class="union-variant-item" id="query"></div>
                            <div class="union-variant-item collapsed" id="eql" hidden="until-found">
                                <div class="nested-properties" hidden="until-found">
                                    <div class="property-item collapsed" id="actions-row">
                                        <dt id="actions">actions</dt>
                                        <div class="nested-properties" hidden="until-found"></div>
                                    </div>
                                </div>
                            </div>
                            <div class="union-variant-item" id="esql" hidden="until-found"></div>
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
        expect(byId('outer').classList.contains('expanded')).toBe(true)
    })

    it('switches a variant chip list to the variant that holds the target', () => {
        revealCollapsedAncestors(byId('actions'))

        expect(byId('eql').hasAttribute('hidden')).toBe(false)
        expect(byId('query').getAttribute('hidden')).toBe('until-found')
        expect(
            document
                .querySelector('[data-chip="eql"]')!
                .classList.contains('is-active')
        ).toBe(true)
    })

    it("leaves the target's own row as it was", () => {
        revealCollapsedAncestors(byId('actions'))

        expect(byId('actions-row').classList.contains('collapsed')).toBe(true)
    })

    it('reports nothing to reveal when the target is already visible', () => {
        byId('outer').classList.remove('collapsed')
        byId('eql').classList.remove('collapsed')
        byId('eql').removeAttribute('hidden')
        byId('query').setAttribute('hidden', 'until-found')
        document
            .querySelector('[data-chip="query"]')!
            .classList.remove('is-active')
        document.querySelector('[data-chip="eql"]')!.classList.add('is-active')

        expect(revealCollapsedAncestors(byId('actions'))).toBe(false)
    })
})
