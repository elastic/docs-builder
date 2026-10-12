// Clones and builds the assembler (dev) site, and the Codex site when VISUAL_CODEX_CONFIG is set, that the visual tests serve:
// npm run visual:prepare [-- assembler|codex ...]. The isolated host serves docs/ live and needs no preparation.
import { assertNavigationPreviewOffForDev, cleanEnv } from './host.mjs'
import { spawnSync } from 'node:child_process'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
const repoRoot = resolve(here, '../../..')
const dll =
	process.env.DOCS_BUILDER_DLL ??
	join(repoRoot, '.artifacts/bin/docs-builder/debug/docs-builder.dll')

function run(args) {
	console.log(`docs-builder ${args.join(' ')}`)
	const result = spawnSync('dotnet', [dll, ...args], {
		cwd: repoRoot,
		env: cleanEnv(),
		stdio: 'inherit',
	})
	if (result.status !== 0) {
		throw new Error(`docs-builder ${args[0]} failed (${result.status})`)
	}
}

const requested = process.argv.slice(2)
const codexConfig = process.env.VISUAL_CODEX_CONFIG
const wants = (host) =>
	(requested.length === 0 && (host !== 'codex' || codexConfig)) ||
	requested.includes(host)

if (wants('assembler')) {
	assertNavigationPreviewOffForDev()
	const dev = [
		'--environment',
		'dev',
		'-c',
		'local',
		'--skip-private-repositories',
	]
	run(['assembler', 'clone', ...dev])
	run(['assembler', 'build', ...dev, '--no-assume-build'])
}

if (wants('codex')) {
	if (!codexConfig) {
		throw new Error(
			'Set VISUAL_CODEX_CONFIG to a Codex config you have access to. Codex content can be private, so no default is used.'
		)
	}
	run(['codex', 'clone', codexConfig])
	run(['codex', 'build', codexConfig])
}
