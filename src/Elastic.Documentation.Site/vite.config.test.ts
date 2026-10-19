/**
 * @jest-environment node
 */
import type { UserConfig } from 'vite'

jest.mock('vite', () => ({ defineConfig: (config: unknown) => config }))
jest.mock('@tailwindcss/vite', () => () => ({ name: 'tailwindcss' }))
jest.mock('@vitejs/plugin-react', () => () => ({ name: 'react' }))
jest.mock('vite-plugin-svgr', () => () => ({ name: 'svgr' }))

const loadConfig = async (argv: string[]): Promise<UserConfig> => {
    const originalArgv = process.argv
    process.argv = ['node', 'vite', ...argv]
    try {
        let config: UserConfig = {}
        await jest.isolateModulesAsync(async () => {
            config = (await import('./vite.config')).default as UserConfig
        })
        return config
    } finally {
        process.argv = originalArgv
    }
}

describe('vite.config build.watch', () => {
    it('leaves watch unset for a one-shot build so `vite build` exits', async () => {
        const config = await loadConfig(['build'])

        expect(config.build?.watch).toBeNull()
    })

    it('excludes the build output under --watch so writes do not trigger a rebuild', async () => {
        const config = await loadConfig(['build', '--watch'])

        expect(config.build?.watch).toEqual(
            expect.objectContaining({
                exclude: expect.arrayContaining(['**/_static/**']),
            })
        )
    })
})
