import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  build: {
    lib: {
      entry: 'src/main.tsx',
      name: 'VectorSizingCalculator',
      fileName: 'vector-sizing-calculator',
      formats: ['iife'],
    },
    rollupOptions: {
      output: {
        inlineDynamicImports: true,
      },
    },
    cssCodeSplit: false,
    cssMinify: true,
  },
  define: {
    'process.env.NODE_ENV': '"production"',
  },
});
