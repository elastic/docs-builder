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
    'ds-layers': 'ds-layers.css',
    'ds-css': 'ds.css',
    'ds-guide': 'ds-guide.css',
}

export default defineConfig({
    base: './',
    plugins: [
        react({ jsxImportSource: '@emotion/react' }),
        svgr(),
        tailwindcss(),
    ],
    define: {
        'process.env.DOCS_BUILDER_VERSION': JSON.stringify(docsBuilderVersion),
    },
    build: {
        // The design system's floor, which Vite lowers nesting and other newer CSS to. The legacy bundle is lowered the same way.
        cssTarget: ['chrome111', 'edge111', 'firefox113', 'safari16.2'],
        outDir: '_static',
        emptyOutDir: false,
        sourcemap: true,
        // Razor resolves `main.js` to the hashed entry through `.vite/manifest.json`.
        manifest: true,
        rollupOptions: {
            input: {
                main: path.resolve(__dirname, 'Assets/main.ts'),
                styles: path.resolve(__dirname, 'Assets/styles.css'),
                assembler: path.resolve(__dirname, 'Assets/assembler.css'),
                isolated: path.resolve(__dirname, 'Assets/isolated.css'),
                codex: path.resolve(__dirname, 'Assets/codex.css'),
                // The design-system bundle, loaded instead of the above when NAVIGATION_PREVIEW is on.
                // ds-layers.css stays its own file: the cascade order must not be reordered by the bundler.
                ds: path.resolve(__dirname, 'Assets/ds.ts'),
                'ds-layers': path.resolve(__dirname, 'Assets/ds-layers.css'),
                'ds-css': path.resolve(__dirname, 'Assets/ds.css'),
                'ds-guide': path.resolve(__dirname, 'Assets/ds-guide.css'),
            },
            output: {
                // Chunks import shared code back from the entry by file name. A content-hashed
                // name keeps that import and the <script> tag on one URL; `main.js?v=hash`
                // next to `./main.js` made the browser evaluate the entry twice.
                entryFileNames: (chunkInfo) => {
                    if (chunkInfo.name === 'main') {
                        return 'main-[hash].js'
                    }

                    return `${chunkInfo.name}.js`
                },
                chunkFileNames: 'chunk-[name]-[hash].js',
                assetFileNames: (assetInfo) => {
                    const originalName =
                        assetInfo.names?.[0] ?? assetInfo.name ?? ''
                    const cssName =
                        cssEntryNames[originalName.replace(/\.css$/, '')]
                    if (cssName) {
                        return cssName
                    }

                    if (originalName.endsWith('.css')) {
                        return 'chunk-[name]-[hash][extname]'
                    }

                    return 'asset-[name]-[hash][extname]'
                },
            },
        },
    },
})
