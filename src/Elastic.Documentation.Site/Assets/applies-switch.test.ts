import { initAppliesSwitch } from './applies-switch'

function syncedSet(name: string): string {
    return `
        <div class="applies-switch">
            <input class="applies-switch-input" id="${name}-stack" name="${name}" type="radio" checked>
            <label class="applies-switch-label" for="${name}-stack" data-sync-group="deployment" data-sync-id="stack">Stack</label>
            <div class="applies-switch-content">stack</div>
            <input class="applies-switch-input" id="${name}-serverless" name="${name}" type="radio">
            <label class="applies-switch-label" for="${name}-serverless" data-sync-group="deployment" data-sync-id="serverless">Serverless</label>
            <div class="applies-switch-content">serverless</div>
        </div>`
}

const input = (id: string) => document.getElementById(id) as HTMLInputElement
const label = (id: string) =>
    document.querySelector(`label[for="${id}"]`) as HTMLLabelElement

describe('synced applies switches across swaps', () => {
    beforeEach(() => {
        window.sessionStorage.clear()
        document.body.innerHTML = syncedSet('a') + syncedSet('b')
    })

    it('selects the same deployment in every switch', () => {
        initAppliesSwitch()
        label('a-serverless').click()

        expect(input('b-serverless').checked).toBe(true)
        expect(input('b-stack').checked).toBe(false)
    })

    it('leaves the labels of a swapped-out page alone', () => {
        initAppliesSwitch()
        const old = input('b-serverless')
        document.body.innerHTML = syncedSet('a') + syncedSet('b')

        initAppliesSwitch()
        label('a-serverless').click()

        expect(input('b-serverless').checked).toBe(true)
        expect(old.checked).toBe(false)
    })
})
