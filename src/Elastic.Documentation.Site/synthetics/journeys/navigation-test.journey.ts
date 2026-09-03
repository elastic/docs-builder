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

/**
 * Walks the main navigation paths and, along the way, verifies the htmx
 * navigation model: boosted links swap #main-container (article + sidebar).
 * A `window` marker set on first load survives an htmx swap but
 * not a full page load, so it distinguishes SPA navigation from a reload.
 */
journey('navigation test', ({ page, params }) => {
    monitor.use({
        id: `elastic-co-docs-nav-${params.environment}-v2`,
        schedule: getSchedule(params.environment),
        tags: [`env:${params.environment}`],
    })

    const docsRoot = params.docsRoot as string
    step(`Go to ${docsRoot}`, async () => {
        await page.goto(docsRoot, {
            timeout: 60000,
            waitUntil: 'domcontentloaded',
        })
        await expect(page).toHaveTitle(/Elastic Docs \| Elastic Docs/)
        await page.evaluate(() => {
            window['__synthNoReload'] = true
        })
    })

    step('Global nav space is reserved before it renders', async () => {
        const breakpoints = [
            { width: 375, expectedHeight: 102 },
            { width: 768, expectedHeight: 110 },
            { width: 1280, expectedHeight: 118 },
        ]

        for (const { width, expectedHeight } of breakpoints) {
            await page.setViewportSize({ width, height: 800 })
            const minHeight = await page
                .locator('#elastic-nav-wrapper')
                .evaluate((element) =>
                    Number.parseFloat(getComputedStyle(element).minHeight)
                )
            expect(minHeight).toBe(expectedHeight)
        }
    })

    step('Click on "Elastic Fundamentals"', async () => {
        await page
            .getByRole('link', { name: 'Elastic Fundamentals' })
            .first()
            .click()
        await expect(page).toHaveURL(`${docsRoot}/get-started`)
        await expect(page).toHaveTitle(/Elastic fundamentals/)
        await expect(
            page.getByRole('heading', { name: 'Elastic fundamentals' })
        ).toBeVisible()
        // Tag the nav tree so later steps can tell preserved DOM from a swap
        await page.evaluate(() => {
            const navTree = document.querySelector('[id^="nav-tree"]')
            if (navTree) navTree['__synthOriginal'] = true
        })
    })

    step(
        'Main content keeps its column while pages nav is absent',
        async () => {
            const placement = await page.evaluate(() => {
                const content =
                    document.querySelector<HTMLElement>('#content-container')
                const grid = content?.parentElement
                const sidebar =
                    grid?.querySelector<HTMLElement>(':scope > .sidebar')
                if (!content || !grid || !sidebar)
                    throw new Error(
                        'Expected documentation layout was not found'
                    )

                const marker = document.createComment('pages-nav-placeholder')
                sidebar.replaceWith(marker)

                const contentRect = content.getBoundingClientRect()
                const gridRect = grid.getBoundingClientRect()
                const firstColumnWidth = Number.parseFloat(
                    getComputedStyle(grid).gridTemplateColumns
                )

                marker.replaceWith(sidebar)
                return {
                    contentLeft: contentRect.left,
                    expectedMinimumLeft: gridRect.left + firstColumnWidth,
                }
            })

            expect(placement.contentLeft).toBeGreaterThanOrEqual(
                placement.expectedMinimumLeft
            )
        }
    )

    step('Click on "deployment options" in nav', async () => {
        await page
            .getByRole('link', { name: 'Deployment options' })
            .first()
            .click()
        await expect(page).toHaveURL(
            `${docsRoot}/get-started/deployment-options`
        )
        await expect(page).toHaveTitle(/Deployment options/)
        await expect(
            page.getByRole('heading', { name: 'Deployment options' })
        ).toBeVisible()

        // Same-group navigation: no reload. The sidebar is hx-preserve'd, so
        // expand/collapse stays; only the article swap is visible.
        const state = await page.evaluate(() => {
            const navTree = document.querySelector('[id^="nav-tree"]')
            return {
                noReload: window['__synthNoReload'] === true,
                navStillPresent: navTree !== null,
            }
        })
        expect(state.noReload).toBe(true)
        expect(state.navStillPresent).toBe(true)
    })

    step('Click on "Elastic Cloud" in markdown content', async () => {
        const treeIdBefore = await page.evaluate(
            () => document.querySelector('[id^="nav-tree"]')?.id
        )
        await page
            .locator('#markdown-content')
            .getByRole('link', { name: 'Elastic Cloud' })
            .first()
            .click()
        await expect(page).toHaveURL(
            `${docsRoot}/deploy-manage/deploy/elastic-cloud`
        )
        await expect(page).toHaveTitle(/Elastic Cloud/)

        // Same Guides tree on both pages. Collapsed folders are detached, so
        // wait for initNav to mark the new current path (that also reattaches
        // the deploy-manage ancestors) before inspecting the live tree.
        await expect(
            page.locator(
                '#pages-nav a.sidebar-link.current[href*="/deploy-manage/"]'
            )
        ).toBeVisible()

        // Cross-group navigation: still no reload. htmx preserves identical
        // trees by id, so only require a fresh node when the tree id changes.
        const state = await page.evaluate(() => {
            const navTree = document.querySelector('[id^="nav-tree"]')
            return {
                noReload: window['__synthNoReload'] === true,
                treeId: navTree?.id,
                treeIsNewNode: navTree?.['__synthOriginal'] === undefined,
            }
        })
        expect(state.noReload).toBe(true)
        if (state.treeId !== treeIdBefore)
            expect(state.treeIsNewNode).toBe(true)
    })

    step('Navigate to reference via top nav', async () => {
        await page
            .locator('#secondary-nav')
            .getByRole('link', { name: 'Reference', exact: true })
            .click()
        await expect(page).toHaveURL(`${docsRoot}/reference`)
    })

    step(
        'Sidebar click reaches Elasticsearch and back restores Reference',
        async () => {
            await page
                .locator('#pages-nav a[href$="/reference/elasticsearch"]')
                .first()
                .click()
            await expect(page).toHaveURL(/\/reference\/elasticsearch/)
            await expect(
                page.locator(
                    '#pages-nav a.sidebar-link.current[href*="/reference/elasticsearch"]'
                )
            ).toBeVisible()

            await page.goBack()
            await expect(page).toHaveURL(/\/reference\/?$/)
            await expect(
                page.locator(
                    '#pages-nav a.sidebar-link.current[href*="/reference"]'
                )
            ).toBeVisible()
        }
    )

    step(
        'Global nav script executed only once across navigations',
        async () => {
            const navJsFetches = await page.evaluate(
                () =>
                    performance
                        .getEntriesByType('resource')
                        .filter((e) => e.name.includes('elastic-nav.js')).length
            )
            // elastic-nav.js lives in <head> (head-support keeps it) and must not
            // re-execute per navigation; 0 covers environments without the global nav
            expect(navJsFetches).toBeLessThanOrEqual(1)
        }
    )

    step('/docs/api link triggers a full page load, not htmx', async () => {
        const apiUrl = `${docsRoot}/api/`
        await page.evaluate((href) => {
            const a = document.createElement('a')
            a.href = href
            a.id = 'synthetic-api-link'
            a.textContent = 'api'
            document.querySelector('#content-container')?.appendChild(a)
        }, apiUrl)
        // A full page load is a navigation request; an htmx request would be an
        // XHR carrying the ?v= cache-buster. Status doesn't matter (404 locally).
        const [request] = await Promise.all([
            page.waitForRequest((req) => req.url().startsWith(apiUrl), {
                timeout: 30000,
            }),
            page.locator('#synthetic-api-link').click(),
        ])
        expect(request.isNavigationRequest()).toBe(true)
        expect(new URL(request.url()).searchParams.has('v')).toBe(false)
    })
})
