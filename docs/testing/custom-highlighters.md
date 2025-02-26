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

## Painless

```painless
int i = (int)5L;
Map m = new HashMap();
HashMap hm = (HashMap)m;
```

```painless
ZonedDateTime zdt1 =
        ZonedDateTime.of(1983, 10, 13, 22, 15, 30, 0, ZoneId.of('Z'));
ZonedDateTime zdt2 =
        ZonedDateTime.of(1983, 10, 17, 22, 15, 35, 0, ZoneId.of('Z'));

if (zdt1.isAfter(zdt2)) {
    // handle condition
}
```

```painless
if (doc.containsKey('start') && doc.containsKey('end')) {

    if (doc['start'].size() > 0 && doc['end'].size() > 0) {

        ZonedDateTime start = doc['start'].value;
        ZonedDateTime end = doc['end'].value;
        long differenceInMillis = ChronoUnit.MILLIS.between(start, end);

        // handle difference in times
    } else {
        // handle fields without values
    }
} else {
    // handle index with missing fields
}
```