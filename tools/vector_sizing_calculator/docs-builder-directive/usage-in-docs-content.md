In the `approximate-knn-search.md` file in docs-content, the directive is used like this
inside the "Ensure data nodes have enough memory" section:

```markdown
### Interactive sizing calculator [_vector_sizing_calculator]

Use the calculator below to estimate disk and off-heap RAM requirements for your
`dense_vector` fields. Enter your vector count, dimensions, element type, index
structure, and quantization to see per-replica and cluster-wide estimates.

:::{vector-sizing-calculator}
:::
```

That's it — the directive outputs `<vector-sizing-calculator></vector-sizing-calculator>`
and the JS bundle handles everything client-side.
