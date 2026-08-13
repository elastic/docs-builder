import { journey, step, monitor, expect } from '@elastic/synthetics'

const MCP_PATH = '/docs/_mcp'
// Content-Type is set automatically by Playwright when `data` is an object; only
// Accept is load-bearing here (required by the MCP streamable-HTTP transport).
const MCP_ACCEPT_HEADER = { Accept: 'application/json, text/event-stream' }

// ponytail: duplicated from navigation-test.journey.ts — a 7-line pure helper across 2 files
// isn't worth a shared module yet; extract to synthetics/lib.ts if a 3rd journey needs it.
function getSchedule(env: string) {
    const scheduleMapping = {
        local: 15,
        edge: 15,
        staging: 15,
        prod: 1,
    }
    return scheduleMapping[env] || 15
}

// MCP server is not hosted on the local docs server (:4000), so only register the journey
// for the deployed environments. env is read at module load, same as synthetics.config.ts.
const env = process.env.DOCS_ENV ?? 'local'
if (env !== 'local') {
    journey('mcp server', ({ request, params }) => {
        monitor.use({
            id: `elastic-co-docs-mcp-${params.environment}-v2`,
            schedule: getSchedule(params.environment),
            tags: [`env:${params.environment}`],
        })

        const mcpUrl = `${params.baseUrl}${MCP_PATH}`

        step('MCP liveness endpoint returns 200', async () => {
            const res = await request.get(`${mcpUrl}/alive`)
            expect(res.status()).toBe(200)
        })

        step('MCP lists its tools', async () => {
            const res = await request.post(mcpUrl, {
                headers: MCP_ACCEPT_HEADER,
                data: {
                    jsonrpc: '2.0',
                    id: 1,
                    method: 'tools/list',
                    params: {},
                },
            })
            expect(res.ok()).toBeTruthy()
            expect(await res.text()).toContain('search_docs')
        })

        step('MCP search_docs returns results', async () => {
            const res = await request.post(mcpUrl, {
                headers: MCP_ACCEPT_HEADER,
                data: {
                    jsonrpc: '2.0',
                    id: 2,
                    method: 'tools/call',
                    params: {
                        name: 'search_docs',
                        arguments: { query: 'elasticsearch' },
                    },
                },
            })
            expect(res.ok()).toBeTruthy()
            const body = await res.text()
            // JSON-RPC success has a "result" field; an error reply has "error" instead.
            expect(body).toContain('"result"')
        })
    })
}
