import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'
import { defineConfig } from 'vite'
import svgr from 'vite-plugin-svgr'

const docsBuilderVersion =
    process.env.DOCS_BUILDER_VERSION?.trim() ?? '0.0.0-dev'

const cssEntryNames: Record<string, string> = {
    styles: 'styles.css',
    assembler: 'assembler.css',
    isolated: 'isolated.css',
    codex: 'codex.css',
}

export default defineConfig({
    plugins: [
        react({ jsxImportSource: '@emotion/react' }),
        svgr(),
        tailwindcss(),
    ],
    define: {
        'process.env.DOCS_BUILDER_VERSION': JSON.stringify(docsBuilderVersion),
    },
    build: {
        outDir: '_static',
        emptyOutDir: false,
        sourcemap: true,
        rollupOptions: {
            input: {
                main: path.resolve(__dirname, 'Assets/main.ts'),
                styles: path.resolve(__dirname, 'Assets/styles.css'),
                assembler: path.resolve(__dirname, 'Assets/assembler.css'),
                isolated: path.resolve(__dirname, 'Assets/isolated.css'),
                codex: path.resolve(__dirname, 'Assets/codex.css'),
            },
            output: {
                entryFileNames: (chunkInfo) => {
                    if (chunkInfo.name === 'main') {
                        return 'main.js'
                    }

                    return `${chunkInfo.name}.js`
                },
                assetFileNames: (assetInfo) => {
                    const originalName =
                        assetInfo.names?.[0] ?? assetInfo.name ?? ''
                    const cssName =
                        cssEntryNames[originalName.replace(/\.css$/, '')]
                    if (cssName) {
                        return cssName
                    }

                    if (originalName.endsWith('.css')) {
                        return '[name][extname]'
                    }

                    return 'assets/[name]-[hash][extname]'
                },
            },
        },
    },
})
