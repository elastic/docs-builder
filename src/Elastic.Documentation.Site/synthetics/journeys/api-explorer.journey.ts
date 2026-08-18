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

        step('Open the API Explorer catalog', async () => {
            const response = await page.goto(`${host}${catalogPath}`, {
                timeout: 60000,
                waitUntil: 'domcontentloaded',
            })
            expect(response?.ok()).toBeTruthy()
            await expect(
                page.getByRole('heading', { name: 'API Explorer' })
            ).toBeVisible()
        })

        step('Open the Elasticsearch API landing page', async () => {
            await page
                .locator('#elastic-docs-v3')
                .getByRole('link', { name: /Elasticsearch/i })
                .first()
                .click()
            await expect(page).toHaveURL(
                new RegExp(`${escapeRegex(host)}/docs/api/doc/elasticsearch/?$`)
            )
        })

        step('Switch API version with the sidebar dropdown', async () => {
            const switcher = page.locator('#api-version-switcher')
            await expect(switcher).toBeVisible()

            const optionCount = await switcher.locator('option').count()
            expect(optionCount).toBeGreaterThan(1)

            const currentValue = await switcher.inputValue()
            const targetValue = await switcher.evaluate((select, current) => {
                const option = Array.from(select.options).find(
                    (entry) => entry.value !== current
                )
                return option?.value ?? null
            }, currentValue)
            expect(targetValue).toBeTruthy()

            const currentUrl = page.url()
            await Promise.all([
                page.waitForURL((url) => url.href !== currentUrl),
                switcher.selectOption(targetValue!),
            ])

            await expect(page).toHaveURL(
                /\/docs\/api\/doc\/elasticsearch\/v\d+\//
            )
            await expect(switcher).toBeVisible()
        })

        step('Open an operation page from the API sidebar', async () => {
            const operationLink = page
                .locator('#pages-nav a[href*="/operation/"]:visible')
                .first()

            // The sidebar renders every group collapsed on a landing page, so walk down the
            // first unexpanded branch until an operation link becomes clickable.
            const collapsedToggle = page
                .locator(
                    '#pages-nav li.nav-folder:visible > div.peer:has(input:not(:checked)) label'
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
                /\/docs\/api\/doc\/elasticsearch\/v\d+\/operation\//
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
