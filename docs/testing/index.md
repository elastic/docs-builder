# Testing

The files in this directory are used for testing purposes. Do not edit these files unless you are working on tests.


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
