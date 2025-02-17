# Additional syntax highlighters


## Console / REST API documentation

::::{tab-set}

:::{tab-item} Output

```console
GET /mydocuments/_search
{
    "from": 1,
    "query": {
        "match_all" {}
    }
}
```

:::

:::{tab-item} Markdown

````markdown
```console
GET /mydocuments/_search
{
    "from": 1,
    "query": {
        "match_all" {}
    }
}
```
````

## EQL

sequence
```eql
sequence
  [ file where file.extension == "exe" ]
  [ process where true ]
```

sequence until

```eql
sequence by ID
  A
  B
until C
```
sample

```eql
sample by host
  [ file where file.extension == "exe" ]
  [ process where true ]
```
head (pipes)
```eql
process where process.name == "svchost.exe"
| tail 5
```
function calls

```eql
modulo(10, 6)
modulo(10, 5)
modulo(10, 0.5)
```