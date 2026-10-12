import { HostName, pages, slug } from './pages'
import { expect, test } from '@playwright/test'

// A pixel change always comes with a reviewable markup diff. The snapshot is the server-rendered response,
// not the hydrated DOM, so runtime-generated Emotion styles and React ids can't make it flaky.
function scrub(html: string): string {
    return (
        html
            .replace(/\?v=[0-9a-f]{16}/g, '?v=HASH')
            .replace(/\.[0-9a-f]{8}\.(js|css|woff2)/g, '.HASH.$1')
            .replace(/localhost:\d+/g, 'localhost:PORT')
            // The Vite build names font assets asset-<name>-<hash> and loads the bundle as a module; this keeps older baselines comparable.
            .replace(
                /\/_static\/\.\/asset-(.+?)-[\w-]{8}\.woff2/g,
                '/_static/$1.HASH.woff2'
            )
            .replace(
                /<script type="module" src="([^"]*\/_static\/main\.js\?v=HASH)"><\/script>/g,
                '<script src="$1" defer></script>'
            )
    )
}

for (const host of Object.keys(pages) as HostName[]) {
    test.describe(host, () => {
        // Playwright requires the fixtures argument to be a destructuring pattern
        // eslint-disable-next-line no-empty-pattern
        test.beforeEach(({}, testInfo) => {
            test.skip(testInfo.project.metadata.host !== host)
        })

        for (const path of pages[host]) {
            test(path, async ({ page }) => {
                await page.goto(path, { waitUntil: 'networkidle' })
                await page.evaluate(() => document.fonts.ready)

                const response = await page.request.get(path)
                expect(scrub(await response.text())).toMatchSnapshot(
                    `${slug(path)}.html`
                )
                await expect(page).toHaveScreenshot(`${slug(path)}.png`, {
                    fullPage: true,
                    animations: 'disabled',
                })
            })
        }
    })
}
