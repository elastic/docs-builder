import { initTabs } from './tabs'

function tabSet(): string {
    return `
        <div class="tabs">
            <input class="tabs-input" id="t0" name="set" type="radio" checked>
            <label class="tabs-label" for="t0">One</label>
            <div class="tabs-content" id="p0">first</div>
            <input class="tabs-input" id="t1" name="set" type="radio">
            <label class="tabs-label" for="t1">Two</label>
            <div class="tabs-content" id="p1">second</div>
            <input class="tabs-input" id="t2" name="set" type="radio">
            <label class="tabs-label" for="t2">Three</label>
            <div class="tabs-content" id="p2">third</div>
        </div>`
}

const input = (id: string) => document.getElementById(id) as HTMLInputElement
const panel = (id: string) => document.getElementById(id) as HTMLElement

/** The object in the prototype chain that defines `onbeforematch`, which is what the feature check looks for. */
function beforeMatchOwner(): object | null {
    let owner: object | null = document
    while (
        owner &&
        !Object.prototype.hasOwnProperty.call(owner, 'onbeforematch')
    )
        owner = Object.getPrototypeOf(owner)
    return owner
}

const supportedOwner = beforeMatchOwner()
const supportedDescriptor = supportedOwner
    ? Object.getOwnPropertyDescriptor(supportedOwner, 'onbeforematch')
    : undefined
// Where support is installed and removed. Without a native definition it is the document itself.
const supportTarget = supportedOwner ?? document

/** jsdom may or may not define beforematch support, so each test sets the browser feature on or off by hand. */
function setUntilFoundSupport(supported: boolean) {
    if (supported && !('onbeforematch' in document))
        Object.defineProperty(
            supportTarget,
            'onbeforematch',
            supportedDescriptor ?? { value: null, configurable: true }
        )
    if (!supported && 'onbeforematch' in document)
        delete (supportTarget as Record<string, unknown>).onbeforematch
}

describe('tabs find-in-page', () => {
    beforeEach(() => {
        setUntilFoundSupport(true)
        document.body.innerHTML = tabSet()
        window.sessionStorage.clear()
        initTabs()
    })

    it('shows the selected panel and hides the others until found', () => {
        expect(panel('p0').hasAttribute('hidden')).toBe(false)
        expect(panel('p1').getAttribute('hidden')).toBe('until-found')
        expect(panel('p2').getAttribute('hidden')).toBe('until-found')
    })

    it('moves the hidden marker when another tab is selected', () => {
        input('t1').checked = true
        input('t1').dispatchEvent(new Event('change', { bubbles: true }))

        expect(panel('p0').getAttribute('hidden')).toBe('until-found')
        expect(panel('p1').hasAttribute('hidden')).toBe(false)
        expect(panel('p2').getAttribute('hidden')).toBe('until-found')
    })

    it('selects the tab that holds a find-in-page match', () => {
        panel('p2').dispatchEvent(new Event('beforematch', { bubbles: true }))

        expect(input('t2').checked).toBe(true)
        expect(panel('p2').hasAttribute('hidden')).toBe(false)
        expect(panel('p0').getAttribute('hidden')).toBe('until-found')
    })

    it('selects the tab when the match is in an element inside the panel', () => {
        panel('p1').innerHTML = '<p><span id="hit">needle</span></p>'

        document
            .getElementById('hit')!
            .dispatchEvent(new Event('beforematch', { bubbles: true }))

        expect(input('t1').checked).toBe(true)
        expect(panel('p1').hasAttribute('hidden')).toBe(false)
    })

    it('registers its document listeners once when initialised again', () => {
        initTabs()
        initTabs()
        panel('p1').dispatchEvent(new Event('beforematch', { bubbles: true }))

        expect(input('t1').checked).toBe(true)
        expect(panel('p1').hasAttribute('hidden')).toBe(false)
    })

    it('leaves the panels alone in a browser without hidden="until-found"', () => {
        setUntilFoundSupport(false)
        document.body.innerHTML = tabSet()

        initTabs()
        input('t1').checked = true
        input('t1').dispatchEvent(new Event('change', { bubbles: true }))

        expect(panel('p0').hasAttribute('hidden')).toBe(false)
        expect(panel('p1').hasAttribute('hidden')).toBe(false)
        expect(panel('p2').hasAttribute('hidden')).toBe(false)
    })
})
