// Clones and builds the assembler (dev) site, and the Codex site when VISUAL_CODEX_CONFIG is set, that the visual tests serve:
// npm run visual:prepare [-- assembler|codex ...]. The isolated host serves docs/ live and needs no preparation.
import { assertNavigationPreviewOffForDev, cleanEnv } from './host.mjs'
import { spawn, spawnSync } from 'node:child_process'
import { existsSync, writeFileSync } from 'node:fs'
import { homedir, tmpdir } from 'node:os'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
// VISUAL_ROOT points at another checkout whose binary, static files and config are used instead of this one, as the seed does for its base ref.
const repoRoot = process.env.VISUAL_ROOT ?? resolve(here, '../../..')
const dll =
	process.env.DOCS_BUILDER_DLL ??
	join(repoRoot, '.artifacts/bin/docs-builder/debug/docs-builder.dll')

// docs-builder has been seen to stay alive for tens of minutes after it logs `build :: Finished`. The cause is not known yet, so a
// build that lingers is stopped after a grace period, and its stacks are saved first so the next occurrence can be diagnosed.
const lingerMs = 60_000

function saveStacks(pid) {
	const tool = join(homedir(), '.dotnet/tools/dotnet-stack')
	if (!existsSync(tool))
		return 'dotnet-stack is not installed (dotnet tool install -g dotnet-stack)'
	const out = join(tmpdir(), `docs-builder-linger-${pid}.stack.txt`)
	const result = spawnSync(tool, ['report', '-p', String(pid)], {
		encoding: 'utf8',
		timeout: 30_000,
	})
	writeFileSync(out, `${result.stdout ?? ''}${result.stderr ?? ''}`)
	return out
}

function run(args) {
	console.log(`docs-builder ${args.join(' ')}`)
	return new Promise((resolvePromise, reject) => {
		const child = spawn('dotnet', [dll, ...args], {
			cwd: repoRoot,
			env: cleanEnv(),
			stdio: ['inherit', 'pipe', 'pipe'],
		})
		let lingerTimer
		const watch = (stream, sink) =>
			stream.on('data', (chunk) => {
				sink.write(chunk)
				if (!lingerTimer && /:: Finished in/.test(chunk.toString())) {
					lingerTimer = setTimeout(() => {
						const stacks = saveStacks(child.pid)
						console.warn(
							`docs-builder ${args[0]} is still running ${lingerMs / 1000}s after it finished. Stacks: ${stacks}. Stopping it.`
						)
						child.kill('SIGKILL')
					}, lingerMs)
				}
			})
		watch(child.stdout, process.stdout)
		watch(child.stderr, process.stderr)
		child.on('exit', (code, signal) => {
			clearTimeout(lingerTimer)
			if (code === 0 || (signal === 'SIGKILL' && lingerTimer))
				resolvePromise()
			else
				reject(
					new Error(
						`docs-builder ${args[0]} failed (${code ?? signal})`
					)
				)
		})
	})
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
	await run(['assembler', 'clone', ...dev])
	await run(['assembler', 'build', ...dev, '--no-assume-build'])
}

if (wants('codex')) {
	if (!codexConfig) {
		throw new Error(
			'Set VISUAL_CODEX_CONFIG to a Codex config you have access to. Codex content can be private, so no default is used.'
		)
	}
	await run(['codex', 'clone', codexConfig])
	await run(['codex', 'build', codexConfig])
}
