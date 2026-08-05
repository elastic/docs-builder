import { useState } from 'react';
import {
  EuiPanel,
  EuiSpacer,
  EuiCodeBlock,
  EuiButtonEmpty,
} from '@elastic/eui';

interface FormulasPanelProps {
  formulas: string[];
}

export function FormulasPanel({ formulas }: FormulasPanelProps) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <EuiPanel paddingSize="l">
      <EuiButtonEmpty
        size="s"
        iconType={isOpen ? 'arrowDown' : 'arrowRight'}
        iconSide="left"
        onClick={() => setIsOpen(!isOpen)}
        flush="left"
      >
        {isOpen ? 'Hide' : 'Show'} formulas used
      </EuiButtonEmpty>
      {isOpen && (
        <>
          <EuiSpacer size="s" />
          <EuiCodeBlock
            language="text"
            fontSize="s"
            paddingSize="m"
            isCopyable
          >
            {formulas.join('\n')}
          </EuiCodeBlock>
        </>
      )}
    </EuiPanel>
  );
}
