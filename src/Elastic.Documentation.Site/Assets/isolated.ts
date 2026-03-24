/**
 * Isolated/Codex header entry point.
 *
 * Built as a separate Parcel target ("isolated-js") and loaded only for
 * Isolated and Codex builds via _Head.cshtml. Registers the
 * <elastic-docs-header> web component without bloating main.js with
 * React / EUI dependencies.
 */
import './web-components/Header/Header'
