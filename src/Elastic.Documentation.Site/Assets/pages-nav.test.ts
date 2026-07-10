import { initNav } from './pages-nav'

function setLocation(pathname: string) {
    window.history.pushState({}, '', pathname)
}

describe('initNav folder expansion', () => {
    beforeEach(() => {
        document.body.innerHTML = `
            <div id="pages-nav">
                <li>
                    <input type="checkbox" id="folder-a">
                    <a href="/a">A</a>
                    <ul>
                        <li><a href="/a/one">A one</a></li>
                    </ul>
                </li>
                <li>
                    <input type="checkbox" id="folder-b">
                    <a href="/b">B</a>
                    <ul>
                        <li><a href="/b/one">B one</a></li>
                    </ul>
                </li>
            </div>
        `
    })

    it('does not accumulate expanded folders across unrelated navigations', () => {
        setLocation('/a/one')
        initNav()
        expect(
            (document.getElementById('folder-a') as HTMLInputElement).checked
        ).toBe(true)

        setLocation('/b/one')
        initNav()
        expect(
            (document.getElementById('folder-a') as HTMLInputElement).checked
        ).toBe(false)
        expect(
            (document.getElementById('folder-b') as HTMLInputElement).checked
        ).toBe(true)
    })
})
