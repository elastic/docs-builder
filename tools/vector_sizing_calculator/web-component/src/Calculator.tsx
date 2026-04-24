import { useState, useMemo, useEffect, useCallback } from 'react';
import { EuiSpacer, EuiText } from '@elastic/eui';
import type {
  ElementType,
  IndexType,
  Quantization,
  CalculatorInputs,
} from './types';
import {
  calculate,
  validate,
  getAvailableQuantizations,
} from './calculations';
import { ConfigurationPanel } from './components/ConfigurationPanel';
import { ResultsPanel } from './components/ResultsPanel';
import { ClusterTotals } from './components/ClusterTotals';
import { FormulasPanel } from './components/FormulasPanel';

function getAvailableIndexTypes(elementType: ElementType) {
  const options = [
    { value: 'hnsw', text: 'HNSW' },
    { value: 'flat', text: 'Flat (brute-force)' },
  ];
  if (elementType === 'float' || elementType === 'bfloat16') {
    options.push({ value: 'disk_bbq', text: 'DiskBBQ' });
  }
  return options;
}

function parseVectorCount(s: string): number {
  if (!s) return NaN;
  const clean = s.trim().replace(/,/g, '');
  const multipliers: Record<string, number> = {
    k: 1e3, K: 1e3, m: 1e6, M: 1e6, b: 1e9, B: 1e9,
  };
  const match = clean.match(/^(\d+\.?\d*)\s*([kKmMbB])?$/);
  if (!match) return parseInt(clean, 10);
  return Math.round(
    parseFloat(match[1]) * (match[2] ? multipliers[match[2]] : 1)
  );
}

export function Calculator() {
  const [vectorsText, setVectorsText] = useState('');
  const [numDimensions, setNumDimensions] = useState<number | string>('');
  const [elementType, setElementType] = useState<ElementType>('float');
  const [indexType, setIndexType] = useState<IndexType>('hnsw');
  const [quantization, setQuantization] = useState<Quantization>('none');
  const [replicas, setReplicas] = useState(1);
  const [hnswM, setHnswM] = useState(16);
  const [efConstruction, setEfConstruction] = useState(100);
  const [vectorsPerCluster, setVectorsPerCluster] = useState(384);

  const indexTypeOptions = useMemo(() => getAvailableIndexTypes(elementType), [elementType]);
  const quantOptions = useMemo(() => getAvailableQuantizations(elementType, indexType), [elementType, indexType]);

  useEffect(() => {
    const available = indexTypeOptions.map((o) => o.value);
    if (!available.includes(indexType)) {
      setIndexType(available[0] as IndexType);
    }
  }, [indexTypeOptions, indexType]);

  useEffect(() => {
    const available = quantOptions.map((o) => o.value);
    if (!available.includes(quantization)) {
      setQuantization(available[0] as Quantization);
    }
  }, [quantOptions, quantization]);

  const inputs: CalculatorInputs = useMemo(() => ({
    numVectors: parseVectorCount(vectorsText),
    numDimensions: typeof numDimensions === 'number' ? numDimensions : NaN,
    elementType,
    indexType,
    quantization,
    replicas,
    hnswM,
    efConstruction,
    vectorsPerCluster,
  }), [vectorsText, numDimensions, elementType, indexType, quantization, replicas, hnswM, efConstruction, vectorsPerCluster]);

  const validation = useMemo(() => validate(inputs), [inputs]);
  const result = useMemo(() => (validation.valid ? calculate(inputs) : null), [inputs, validation]);

  const handleVectorsBlur = useCallback(() => {
    const v = parseVectorCount(vectorsText);
    if (!isNaN(v) && v > 0) {
      setVectorsText(v.toLocaleString('en-US'));
    }
  }, [vectorsText]);

  return (
    <>
      <ConfigurationPanel
        vectorsText={vectorsText}
        onVectorsChange={setVectorsText}
        onVectorsBlur={handleVectorsBlur}
        numDimensions={numDimensions}
        onDimensionsChange={setNumDimensions}
        elementType={elementType}
        onElementTypeChange={setElementType}
        indexType={indexType}
        onIndexTypeChange={setIndexType}
        indexTypeOptions={indexTypeOptions}
        quantization={quantization}
        onQuantizationChange={setQuantization}
        quantOptions={quantOptions}
        replicas={replicas}
        onReplicasChange={setReplicas}
        hnswM={hnswM}
        onHnswMChange={setHnswM}
        efConstruction={efConstruction}
        onEfConstructionChange={setEfConstruction}
        vectorsPerCluster={vectorsPerCluster}
        onVectorsPerClusterChange={setVectorsPerCluster}
        validation={validation}
      />

      <EuiSpacer size="m" />
      <ResultsPanel result={result} />

      {result && replicas > 0 && (
        <>
          <EuiSpacer size="m" />
          <ClusterTotals result={result} replicas={replicas} />
        </>
      )}

      {result && (
        <>
          <EuiSpacer size="m" />
          <FormulasPanel formulas={result.formulas} />
        </>
      )}

      <EuiSpacer size="s" />
      <EuiText size="xs" color="subdued">
        <em>
          Estimates are approximate — run benchmarks with your specific
          dataset for production sizing.
        </em>
      </EuiText>
    </>
  );
}
