/**
 * Codex build entry point.
 *
 * Built as a separate Parcel target ("codex-js") and loaded only for
 * Codex builds via _Head.cshtml. Registers the <codex-search-bar>
 * web component without bloating main.js with React / EUI dependencies.
 */
import './web-components/CodexHeader/SearchBar'
