# Testing

The files in this directory are used for testing purposes. Do not edit these files unless you are working on tests.

test

###### [#synthetics-config-file]

% [Non Existing Link](./non-existing.md)

```json
{
  "key": "value"
}
```

  ```json
  {
    "key": "value"
  }
  ```

1. this is a list
   ```json
      {
        "key": "value"
      }
   ```
   1. this is a sub-list
      ```json
      {
        "key": "value"
      }
      ```

```console
PUT metricbeat-2016.05.30/_doc/1?refresh <1>
{"system.cpu.idle.pct": 0.908}
PUT metricbeat-2016.05.31/_doc/1?refresh <2>
{"system.cpu.idle.pct": 0.105}
```
1. test 1
2. test 2

```console
POST _reindex
{
  "max_docs": 10,
  "source": {
    "index": "my-index-000001",
    "query": {
      "function_score" : {
        "random_score" : {},
        "min_score" : 0.9
      }
    }
  },
  "dest": {
    "index": "my-new-index-000001"
  }
}
```

```console
GET metricbeat-2016.05.30-1/_doc/1
GET metricbeat-2016.05.31-1/_doc/1
```

```console
PUT my-index-000001
{
  "mappings": {
    "enabled": false <1>
  }
}

PUT my-index-000001/_doc/session_1
{
  "user_id": "kimchy",
  "session_data": {
    "arbitrary_object": {
      "some_array": [ "foo", "bar", { "baz": 2 } ]
    }
  },
  "last_updated": "2015-12-06T18:20:22"
}

GET my-index-000001/_doc/session_1 <2>

GET my-index-000001/_mapping <3>
```

1. The entire mapping is disabled.
2. The document can be retrieved.
3. Checking the mapping reveals that no fields have been added.

```javascript
const foo = "bar"; <1>
```

1. This is a JavaScript code block.
