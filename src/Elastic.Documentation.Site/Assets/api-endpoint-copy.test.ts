import { initApiDocs } from './api-docs'

function alternativePathsMarkup(): string {
    return `
        <section id="elastic-api-v3">
            <div class="api-operation-description">
                <div>
                    <span class="operation-verb put">PUT</span>
                    <span class="operation-path">/{index}/_create/{id}</span>
                </div>
                <div>
                    <span class="operation-verb post">POST</span>
                    <span class="operation-path">/_create</span>
                </div>
            </div>
        </section>
    `
}

describe('alternative path copy', () => {
    const writeText = jest.fn().mockResolvedValue(undefined)

    beforeAll(() => {
        Object.defineProperty(navigator, 'clipboard', {
            value: { writeText },
            configurable: true,
        })
    })

    beforeEach(() => {
        writeText.mockClear()
        document.body.innerHTML = alternativePathsMarkup()
        initApiDocs()
    })

    it('adds a copy button beside each path chip', () => {
        const rows = document.querySelectorAll('.operation-path-row')

        expect(rows).toHaveLength(2)
        rows.forEach((row) => {
            expect(
                row.firstElementChild?.querySelector('.operation-path')
            ).not.toBeNull()
            expect(row.lastElementChild?.matches('button.api-url-copy')).toBe(
                true
            )
        })
    })

    it('does not add a second button when the view initialises again', () => {
        initApiDocs()

        expect(document.querySelectorAll('button.api-url-copy')).toHaveLength(2)
    })

    it('copies the path when the path text is clicked', () => {
        document.querySelectorAll<HTMLElement>('.operation-path')[1].click()

        expect(writeText).toHaveBeenCalledWith('/_create')
    })

    it('copies the path when the button is clicked', () => {
        document
            .querySelector<HTMLButtonElement>('button.api-url-copy')!
            .click()

        expect(writeText).toHaveBeenCalledWith('/{index}/_create/{id}')
    })
})
