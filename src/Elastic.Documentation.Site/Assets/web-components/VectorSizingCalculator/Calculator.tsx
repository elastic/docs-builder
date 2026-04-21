import { useState, useMemo, useEffect } from 'react';
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
    getQuantizationInsightText,
} from './calculations';
import { parseVectorCount } from './parseVectorCount';
import { ConfigurationPanel } from './components/ConfigurationPanel';
import { ResultsPanel } from './components/ResultsPanel';

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

export function Calculator() {
    const [vectorsText, setVectorsText] = useState('1,000,000');
    const [numDimensions, setNumDimensions] = useState<number | string>(768);
    const [elementType, setElementType] = useState<ElementType>('float');
    const [indexType, setIndexType] = useState<IndexType>('hnsw');
    const [quantization, setQuantization] = useState<Quantization>('bbq');
    const [replicas, setReplicas] = useState(2);
    const [hnswM, setHnswM] = useState(4);
    const efConstruction = 100;
    const vectorsPerCluster = 384;

    const indexTypeOptions = useMemo(
        () => getAvailableIndexTypes(elementType),
        [elementType]
    );
    const quantOptions = useMemo(
        () => getAvailableQuantizations(elementType, indexType),
        [elementType, indexType]
    );

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

    const inputs: CalculatorInputs = useMemo(
        () => ({
            numVectors: parseVectorCount(vectorsText),
            numDimensions:
                typeof numDimensions === 'number' ? numDimensions : NaN,
            elementType,
            indexType,
            quantization,
            replicas,
            hnswM,
            efConstruction,
            vectorsPerCluster,
        }),
        [
            vectorsText,
            numDimensions,
            elementType,
            indexType,
            quantization,
            replicas,
            hnswM,
            efConstruction,
            vectorsPerCluster,
        ]
    );

    const validation = useMemo(() => validate(inputs), [inputs]);
    const result = useMemo(
        () => (validation.valid ? calculate(inputs) : null),
        [inputs, validation]
    );

    const quantizationLabel = useMemo(
        () =>
            quantOptions.find((o) => o.value === quantization)?.label ?? '',
        [quantOptions, quantization]
    );

    const inputsValid = Boolean(validation.valid && result !== null);

    const quantizationInsightText = useMemo(() => {
        if (!validation.valid || result === null) return null;
        return getQuantizationInsightText(inputs, result);
    }, [inputs, result, validation.valid]);

    return (
        <div className="vectorSizingCalc">
            <div className="vectorSizingCalc__grid">
                <ConfigurationPanel
                    vectorsText={vectorsText}
                    onVectorsChange={setVectorsText}
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
                    validation={validation}
                />

                <ResultsPanel
                    result={result}
                    inputsValid={inputsValid}
                    quantizationLabel={quantizationLabel}
                    replicas={replicas}
                    quantization={quantization}
                    validation={validation}
                    quantizationInsightText={quantizationInsightText}
                />
            </div>
        </div>
    );
}
