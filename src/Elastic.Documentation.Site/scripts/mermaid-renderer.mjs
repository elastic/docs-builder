// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

/**
 * CLI tool to render Mermaid diagrams to SVG using beautiful-mermaid.
 * Reads Mermaid code from stdin and outputs SVG to stdout.
 *
 * Usage: echo "graph LR; A --> B" | node mermaid-renderer.mjs
 */

import { renderMermaid, DEFAULTS } from 'beautiful-mermaid';

async function main() {
	// Read from stdin
	const chunks = [];
	for await (const chunk of process.stdin) {
		chunks.push(chunk);
	}
	const mermaidCode = Buffer.concat(chunks).toString('utf-8').trim();

	if (!mermaidCode) {
		console.error('Error: No Mermaid code provided via stdin');
		process.exit(1);
	}

	try {
		const svg = await renderMermaid(mermaidCode, DEFAULTS);
		process.stdout.write(svg);
	} catch (error) {
		console.error(`Error rendering Mermaid diagram: ${error.message}`);
		process.exit(1);
	}
}

main();
