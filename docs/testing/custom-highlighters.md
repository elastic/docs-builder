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
::::

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



 ## ESQL


```esql
FROM employees
| LIMIT 1000
```

```esql
ROW a = "2023-01-23T12:15:00.000Z - some text - 127.0.0.1"
| DISSECT a """%{date} - %{msg} - %{ip}"""
| KEEP date, msg, ip
```

```esql
FROM books
| WHERE KQL("author: Faulkner")
| KEEP book_no, author
| SORT book_no
| LIMIT 5
```

```esql
FROM hosts
| STATS COUNT_DISTINCT(ip0), COUNT_DISTINCT(ip1)
```

```esql
ROW message = "foo ( bar"
| WHERE message RLIKE "foo \\( bar"
```

```esql
FROM books
| WHERE author:"Faulkner"
| KEEP book_no, author
| SORT book_no
| LIMIT 5;
```
