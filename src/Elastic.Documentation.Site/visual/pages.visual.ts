import { HostName, pages, slug } from './pages'
import { expect, test, type Page } from '@playwright/test'

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
            // The entry bundle is content-hashed (main-[hash].js, ds-[hash].js), so its name changes with every build.
            .replace(/\/_static\/(main|ds)-[\w-]+\.js/g, '/_static/$1.HASH.js')
    )
}

// The isolated host builds the OpenAPI models on the first /api request, which can take over a minute on a busy machine.
const slowPaths = new Set(['/api'])

// The web components this repository defines, which the page loads after its script has initialised. Others, such as the
// elastic.co header, come from an external script that never loads here, so waiting for them would only stall every page.
const ownComponents = [
    'filterable-table',
    'version-dropdown',
    'applies-to-popover',
    'page-feedback',
    'navigation-search',
    'vector-sizing-calculator',
]

// Lazily loaded components render after the network goes idle. Wait for each one the page uses to be defined, then for the page
// height to stop changing, so a screenshot never captures the gap where a component is about to appear.
async function settleIslands(page: Page) {
    await page.evaluate(async (tags) => {
        const used = tags.filter((tag) => document.querySelector(tag))
        await Promise.all(used.map((tag) => customElements.whenDefined(tag)))
    }, ownComponents)

    let last = -1
    let stable = 0
    for (let attempt = 0; attempt < 120 && stable < 4; attempt++) {
        const height = await page.evaluate(
            () => document.documentElement.scrollHeight
        )
        stable = height === last ? stable + 1 : 0
        last = height
        await page.waitForTimeout(250)
    }
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
                test.setTimeout(slowPaths.has(path) ? 180_000 : 30_000)
                await page.goto(path, { waitUntil: 'networkidle' })
                await page.evaluate(() => document.fonts.ready)
                await settleIslands(page)

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
