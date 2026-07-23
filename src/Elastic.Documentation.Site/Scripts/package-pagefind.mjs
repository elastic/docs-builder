import { Buffer } from 'node:buffer'
import { execFileSync } from 'node:child_process'
import { createHash } from 'node:crypto'
import {
	existsSync,
	mkdirSync,
	mkdtempSync,
	readFileSync,
	rmSync,
	writeFileSync,
} from 'node:fs'
import { tmpdir } from 'node:os'
import { dirname, join } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'
import { gzipSync } from 'node:zlib'

const platform = process.platform === 'win32' ? 'windows' : process.platform
const target = {
	'darwin-arm64': 'aarch64-apple-darwin',
	'darwin-x64': 'x86_64-apple-darwin',
	'linux-arm64': 'aarch64-unknown-linux-musl',
	'linux-x64': 'x86_64-unknown-linux-musl',
	'windows-arm64': 'aarch64-pc-windows-msvc',
	'windows-x64': 'x86_64-pc-windows-msvc',
}[`${platform}-${process.arch}`]

if (
	!['darwin', 'linux', 'windows'].includes(platform) ||
	!target
) {
	throw new Error(
		`Pagefind is not packaged for ${process.platform}-${process.arch}`
	)
}

const root = dirname(dirname(fileURLToPath(import.meta.url)))
const { version } = JSON.parse(
	readFileSync(join(root, 'node_modules', 'pagefind', 'package.json'), 'utf8')
)
const archiveName = `pagefind-v${version}-${target}.tar.gz`
const archive = join(root, 'obj', archiveName)
const checksumFile = join(
	root,
	'node_modules',
	'@pagefind',
	`${platform}-${process.arch}`,
	'bin',
	`${archiveName}.sha256`
)
const destination = join(root, 'obj', 'pagefind-indexer.gz')

mkdirSync(dirname(destination), { recursive: true })
if (!existsSync(archive)) {
	const response = await fetch(
		`https://github.com/Pagefind/pagefind/releases/download/v${version}/${archiveName}`
	)
	if (!response.ok) {
		throw new Error(
			`Could not download Pagefind ${version}: ${response.status} ${response.statusText}`
		)
	}
	writeFileSync(archive, Buffer.from(await response.arrayBuffer()))
}

const expectedChecksum = readFileSync(checksumFile, 'utf8').split(/\s+/)[0]
const actualChecksum = createHash('sha256')
	.update(readFileSync(archive))
	.digest('hex')
if (actualChecksum !== expectedChecksum) {
	throw new Error(
		`Pagefind checksum mismatch: expected ${expectedChecksum}, got ${actualChecksum}`
	)
}

const extracted = mkdtempSync(join(tmpdir(), 'pagefind-'))
try {
	execFileSync('tar', ['-xzf', archive, '-C', extracted])
	const executable = process.platform === 'win32' ? 'pagefind.exe' : 'pagefind'
	writeFileSync(
		destination,
		gzipSync(readFileSync(join(extracted, executable)), { level: 9 })
	)
} finally {
	rmSync(extracted, { recursive: true, force: true })
}
