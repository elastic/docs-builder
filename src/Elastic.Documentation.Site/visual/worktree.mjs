// Runs a function with a temporary detached git worktree of a ref, and always removes it afterwards.
import { spawnSync } from 'node:child_process'
import { mkdtempSync, realpathSync, rmSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
export const repoRoot = resolve(here, '../../..')

export function git(args) {
	const result = spawnSync('git', args, { cwd: repoRoot, encoding: 'utf8' })
	if (result.status !== 0) {
		throw new Error(`git ${args.join(' ')} failed: ${result.stderr}`)
	}
	return result.stdout.trim()
}

export async function withWorktree(ref, work) {
	// The binary refuses paths that are outside its workspace as written, so use the real path (on macOS /var is a link to /private/var).
	const dir = realpathSync(mkdtempSync(join(tmpdir(), 'visual-wt-')))
	try {
		git(['worktree', 'add', '--detach', dir, ref])
		return await work(dir)
	} finally {
		spawnSync('git', ['worktree', 'remove', '--force', dir], {
			cwd: repoRoot,
		})
		rmSync(dir, { recursive: true, force: true })
	}
}
