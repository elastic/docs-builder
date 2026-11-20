import { journey, step, monitor, expect } from '@elastic/synthetics'

function getSchedule(env: string) {
    const scheduleMapping = {
        local: 15,
        edge: 15,
        staging: 15,
        prod: 1,
    }
    return scheduleMapping[env] || 15
}

// Match FeatureFlags.IsEnabled("assembler-api-explorer"): FEATURE_ASSEMBLER_API_EXPLORER
// set to a bool wins; any other non-empty value counts as enabled.
function isAssemblerApiExplorerEnabled() {
    const value = process.env.FEATURE_ASSEMBLER_API_EXPLORER
    if (value === undefined || value === '') return false
    if (value.toLowerCase() === 'true') return true
    if (value.toLowerCase() === 'false') return false
    return true
}

if (isAssemblerApiExplorerEnabled()) {
    journey('api explorer', ({ page, params }) => {
        monitor.use({
            id: `elastic-co-docs-api-${params.environment}-v1`,
            schedule: getSchedule(params.environment),
            tags: [`env:${params.environment}`, 'area:api-explorer'],
        })

        const host = params.baseUrl
        const catalogPath = '/docs/api/'

        step('Open the API catalog', async () => {
            const response = await page.goto(`${host}${catalogPath}`, {
                timeout: 60000,
                waitUntil: 'domcontentloaded',
            })
            expect(response?.ok()).toBeTruthy()
            await expect(
                page.getByRole('heading', { name: 'Elastic APIs' })
            ).toBeVisible()
        })

        step('Open the Elasticsearch API landing page', async () => {
            // The catalog links to the versionless URL, which serves the newest released major.
            await page
                .locator('#elastic-docs-v3')
                .getByRole('link', { name: /Elasticsearch/i })
                .first()
                .click()
            await expect(page).toHaveURL(
                new RegExp(`${escapeRegex(host)}/docs/api/elasticsearch/?$`)
            )
        })

        step('Switch API version with the top-bar dropdown', async () => {
            const picker = page.locator(
                '[data-testid="docs-version-dropdown"]:visible'
            )
            await expect(picker).toBeVisible()

            await picker.locator('button').click()
            // The newest major links to the versionless URL and main to /next, so the first /v link is an older major.
            const target = page
                .locator('a[href*="/docs/api/elasticsearch/v"]')
                .first()
            await expect(target).toBeVisible()

            const currentUrl = page.url()
            await Promise.all([
                page.waitForURL((url) => url.href !== currentUrl),
                target.click(),
            ])

            await expect(page).toHaveURL(/\/docs\/api\/elasticsearch\/v\d+\//)
            await expect(picker).toBeVisible()
        })

        step('Open an operation page from the API sidebar', async () => {
            const operationLink = page
                .locator('#pages-nav a[href*="/operation/"]:visible')
                .first()

            // The sidebar renders every group collapsed on a landing page, so walk down the
            // first unexpanded branch until an operation link becomes clickable.
            const collapsedToggle = page
                .locator(
                    '#pages-nav li.nav-folder:visible > div.peer:has(input:not(:checked)) > a.sidebar-link'
                )
                .first()
            for (let depth = 0; depth < 8; depth++) {
                if ((await operationLink.count()) > 0) break
                await expect(collapsedToggle).toBeVisible()
                await collapsedToggle.click()
            }

            await expect(operationLink).toBeVisible()
            await operationLink.click()

            await expect(page).toHaveURL(
                /\/docs\/api\/elasticsearch\/v\d+\/operation\//
            )

            // Operation pages render their own section, not the markdown one, and the
            // reference content a reader comes for is the method and route under "Paths".
            const operationPage = page.locator('#elastic-api-v3')
            await expect(
                operationPage.locator('.api-url-listing .api-method').first()
            ).toHaveText(/^(GET|PUT|POST|DELETE|HEAD|PATCH)$/)
            await expect(
                operationPage.locator('.api-url-listing .api-url').first()
            ).toHaveText(/^\//)
        })
    })
}

function escapeRegex(value: string) {
    return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}
