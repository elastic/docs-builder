import { applyEditParam } from './edit-param'

describe('applyEditParam', () => {
    beforeEach(() => {
        document.body.innerHTML = '<a class="edit-this-page hidden"></a>'
    })

    afterEach(() => {
        window.history.replaceState({}, '', '/')
    })

    it('shows the edit link when the URL has an edit parameter', () => {
        window.history.replaceState({}, '', '/?edit')

        applyEditParam()

        expect(
            document
                .querySelector('.edit-this-page')
                ?.classList.contains('hidden')
        ).toBe(false)
    })

    it('leaves the edit link hidden otherwise', () => {
        applyEditParam()

        expect(
            document
                .querySelector('.edit-this-page')
                ?.classList.contains('hidden')
        ).toBe(true)
    })
})
