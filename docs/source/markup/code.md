---
title: Code
---

You can use the regular markdown code block:

```yaml
project:
  title: MyST Markdown 
  github: https://github.com/jupyter-book/mystmd
  license:
    code: MIT
    content: CC-BY-4.0
  subject: MyST Markdown
```

But you can also use the [code directive](https://mystmd.org/guide/code) that supposedly give you more features.

```{code} yaml
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
  license:
    code: MIT
    content: CC-BY-4.0
  subject: MyST Markdown
```

For now we only support the `caption` option on the `{code}` or `{code-block}`

```{code-block} yaml
:caption: How to configure `license` of a project
project:
  title: MyST Markdown
  github: https://github.com/jupyter-book/mystmd
  license:
    code: MIT
    content: CC-BY-4.0
  subject: MyST Markdown
```

## Code Callouts

### YAML

```yaml
project:
  title: MyST Markdown #1
  github: https://github.com/jupyter-book/mystmd
  license:
    code: MIT
    content: CC-BY-4.0
  subject: MyST Markdown
```

### Java

```java
// Create the low-level client
RestClient restClient = RestClient
    .builder(HttpHost.create(serverUrl)) //1
    .setDefaultHeaders(new Header[]{
        new BasicHeader("Authorization", "ApiKey " + apiKey)
    })
    .build();
```

### Javascript

```javascript
const { Client } = require('@elastic/elasticsearch')
const client = new Client({
  cloud: {
    id: '<cloud-id>' //1
  },
  auth: {
    username: 'elastic',
    password: 'changeme'
  }
})
```

### Ruby

```ruby
require 'elasticsearch'

client = Elasticsearch::Client.new(
  cloud_id: '<CloudID>'
  user: '<Username>', #1
  password: '<Password>',
)
```

### Go

```go
cfg := elasticsearch.Config{
        CloudID: "CLOUD_ID", //1
        APIKey: "API_KEY"
}
es, err := elasticsearch.NewClient(cfg)
```

### C#

```csharp
var apiKey = new ApiKey("<API_KEY>"); //1
var client = new ElasticsearchClient("<CLOUD_ID>", apiKey); 
```

### PHP

```php
$hosts = [
    '192.168.1.1:9200',         //1
    '192.168.1.2',              // Just IP
    'mydomain.server.com:9201', // Domain + Port
    'mydomain2.server.com',     // Just Domain
    'https://localhost',        // SSL to localhost
    'https://192.168.1.3:9200'  // SSL to IP + Port
];
$client = ClientBuilder::create()           // Instantiate a new ClientBuilder
                    ->setHosts($hosts)      // Set the hosts
                    ->build();              // Build the client object
```

### Perl

```perl
my $e = Search::Elasticsearch->new( #1
    nodes => [ 'https://my-test.es.us-central1.gcp.cloud.es.io' ],
    elastic_cloud_api_key => 'insert here the API Key'
);
```
### Python

```python
from elasticsearch import Elasticsearch

ELASTIC_PASSWORD = "<password>" #1

# Found in the 'Manage Deployment' page
CLOUD_ID = "deployment-name:dXMtZWFzdDQuZ2Nw..."

# Create the client instance
client = Elasticsearch(
    cloud_id=CLOUD_ID,
    basic_auth=("elastic", ELASTIC_PASSWORD)
)

# Successful response!
client.info()
# {'name': 'instance-0000000000', 'cluster_name': ...}
```
### Rust

```rust
let url = Url::parse("https://example.com")?; //1
let conn_pool = SingleNodeConnectionPool::new(url);
let transport = TransportBuilder::new(conn_pool).disable_proxy().build()?;
let client = Elasticsearch::new(transport);
```