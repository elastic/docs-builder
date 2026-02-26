/**
 * Codex build entry point.
 *
 * Built as a separate Parcel target ("codex-js") and loaded only for
 * Codex builds via _Head.cshtml. Registers the <elastic-docs-header>
 * web component without bloating main.js with React / EUI dependencies.
 */
import './web-components/CodexHeader/Header'
import './web-components/ModalSearch/ModalSearchComponent'
