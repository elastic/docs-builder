/**
 * Web Component wrapper for the Vector Sizing Calculator.
 *
 * Registers <vector-sizing-calculator> as a custom element.
 * Uses light DOM so the component inherits any page-level EUI styles,
 * and also works standalone when EUI is bundled.
 */

// Pre-cache EUI icons before any component renders (avoids dynamic import failures in Vite)
import './icon-cache';

import React from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { EuiProvider } from '@elastic/eui';
import { Calculator } from './Calculator';

class VectorSizingCalculatorElement extends HTMLElement {
  private _root: Root | null = null;

  connectedCallback() {
    // Render into light DOM (no shadow root) so EUI styles apply
    this._root = createRoot(this);
    this._root.render(
      <React.StrictMode>
        <EuiProvider colorMode="light">
          <Calculator />
        </EuiProvider>
      </React.StrictMode>
    );
  }

  disconnectedCallback() {
    // Cleanup React tree when element is removed from DOM
    if (this._root) {
      this._root.unmount();
      this._root = null;
    }
  }
}

// Only register once (safe for HMR and multiple script loads)
if (!customElements.get('vector-sizing-calculator')) {
  customElements.define(
    'vector-sizing-calculator',
    VectorSizingCalculatorElement
  );
}
