---
navigation_title: Find your team's workflow
---
# Find the workflow for your API docs

:::{warning}
This is WIP and apart from the quick reference is just a quick-and-dirty copy/paste from the Confluence doc
:::

Quickly find the team-specific workflows related to creating, reviewing, and publishing API docs for various Elastic products. Each team has its own unique process.

:::{important}
The bulk of the content on this page is relevant to Elastic employees only. Most links are to internal resources or private repos.
:::

## Quick reference

| Product | Repo | OpenAPI spec file | Live docs | Publishing process | Automated? | Status |
|---------|------------|---------------------|-----------|-------------------|------------|--------|
| Elasticsearch | [elasticsearch-specification](https://github.com/elastic/elasticsearch-specification) | [elasticsearch-openapi.json](https://github.com/elastic/elasticsearch-specification/blob/main/output/openapi/elasticsearch-openapi.json) | [elasticsearch](https://www.elastic.co/docs/api/doc/elasticsearch) | Manual generation + Bump.sh deployment | No | Partial; docs-specific files are not checked in |
| Elasticsearch Serverless | [elasticsearch-specification](https://github.com/elastic/elasticsearch-specification) | [elasticsearch-serverless-openapi.json](https://github.com/elastic/elasticsearch-specification/blob/main/output/openapi/elasticsearch-serverless-openapi.json) | [elasticsearch-serverless](https://www.elastic.co/docs/api/doc/elasticsearch-serverless) | Manual generation + Bump.sh deployment | No | Partial; docs-specific files are not checked in |
| Kibana | [kibana/oas_docs](https://github.com/elastic/kibana/tree/main/oas_docs) | [bundle.json](https://github.com/elastic/kibana/blob/main/oas_docs/bundle.json) | [kibana](https://www.elastic.co/docs/api/doc/kibana) | Auto-generated, manual release | Partial | Full |
| Kibana Serverless | [kibana/oas_docs](https://github.com/elastic/kibana/tree/main/oas_docs) | [bundle.serverless.json](https://github.com/elastic/kibana/blob/main/oas_docs/bundle.serverless.json) | [serverless](https://www.elastic.co/docs/api/doc/serverless) | Auto-generated, manual release | Partial | Full |
| Elastic Cloud | [cloud repo](https://github.com/elastic/cloud) | [apidocs-user.json](https://github.com/elastic/cloud/blob/master/scala-services/adminconsole/src/main/resources/apidocs-user.json) | [cloud](https://www.elastic.co/docs/api/doc/cloud) | Manual overlay + Bump.sh deployment | No | Partial; Missing overlays |
| Elastic Cloud Enterprise | [cloud repo](https://github.com/elastic/cloud) | [apidocs.json](https://github.com/elastic/cloud/blob/master/scala-services/adminconsole/src/main/resources/apidocs.json) | [cloud-enterprise](https://www.elastic.co/docs/api/doc/cloud-enterprise) | Manual overlay + Bump.sh deployment | No | Partial; Missing overlays |
| Elastic Cloud Billing | [cloud repo](https://github.com/elastic/cloud) | [billing-service.external.yaml](https://github.com/elastic/cloud/blob/master/python-services-v3/openapi/billing-service.external.yaml) | [cloud-billing](https://www.elastic.co/docs/api/doc/cloud-billing) | Direct file upload to Bump.sh | No | Full |
| Elastic Cloud Serverless | Multiple repos | Multiple files | [elastic-cloud-serverless](https://www.elastic.co/docs/api/doc/elastic-cloud-serverless) | Fully automated | Yes | Full |
| Logstash | [logstash repo](https://github.com/elastic/logstash) | [logstash-api.yaml](https://github.com/elastic/logstash/blob/main/docs/static/spec/openapi/logstash-api.yaml) | [logstash](https://www.elastic.co/docs/api/doc/logstash) | Manual Bump.sh deployment | No | Full |
| Observability Intake | [apm-managed-service](https://github.com/elastic/apm-managed-service) | [bundled-apm-mis-openapi.json](https://github.com/elastic/apm-managed-service/blob/main/docs/spec/openapi/bundled-apm-mis-openapi.json) | [observability-serverless](https://www.elastic.co/docs/api/doc/observability-serverless) | Manual generation + Bump.sh deployment | No | Full |

## Processes

Most of the openAPI documents are not yet automatically published. Until that is the case, they must be manually published as described in the following sections. For a related docs engineering page, check out [https://elasticco.atlassian.net/wiki/spaces/DE/pages/746193088](https://elasticco.atlassian.net/wiki/spaces/DE/pages/746193088) (Elastic employees only). 

### Elastic Cloud and Elastic Cloud Enterprise

:::{note}
This process only applies overlays to existing generated OpenAPI spec files (authored in Scala code annotations). Regenerating these spec files requires a complete Cloud development environment. If you need to make changes to endpoint or property descriptions, or author new descriptions, then you might need to ask a cloud developer to regenerate the spec for you from your branch. For more details, refer to [https://elasticco.atlassian.net/wiki/spaces/DOC/pages/61605944](https://elasticco.atlassian.net/wiki/spaces/DOC/pages/61605944) (Elastic employees only).
:::

To publish content to [https://www.elastic.co/docs/api/doc/cloud](https://www.elastic.co/docs/api/doc/cloud) and [https://www.elastic.co/docs/api/doc/cloud-enterprise](https://www.elastic.co/docs/api/doc/cloud-enterprise): 

1. Check out the appropriate branch in the [cloud repo](https://github.com/elastic/cloud) (i.e. whatever ms-xxx branch is current according to the [release schedule](https://docs.google.com/spreadsheets/d/1ohPWWkUy5Cc2k4HJSx3XBKXY5nxeuiehApFLFHdHM3U/edit?gid=1929311770#gid=1929311770) (Elastic employees only)).
    
2. Generate the API document by using [Makefile](https://github.com/elastic/cloud/blob/master/docs/Makefile) commands:
    
    1. Go to the `docs` folder
        
    2. Run `make api-docs-overlay`  
        The output files are:
        
        - Elastic Cloud: `docs/openapi/output/apidocs-user.new.json`
            
        - Elastic Cloud Enterprise: `docs/openapi/output/apidocs.new.json`  
            If you encounter an error you might need to create that output directory then re-run the command.
            
    3. Run `make api-docs-lint`  
        If there are errors, they should be addressed before deployment. Warnings and info messages can be ignored.
        
3. Get assistance from someone with admin authority in Bump.sh (reach out on #next-api-reference Slack channel) to deploy the new output file as described in [https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/](https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/) and [https://docs.google.com/document/d/1xP_n2AusBhJcLmmJeCZZkAztzeykvkZtJ6la2XHz7Aw/edit?tab=t.0](https://docs.google.com/document/d/1xP_n2AusBhJcLmmJeCZZkAztzeykvkZtJ6la2XHz7Aw/edit?tab=t.0) (Elastic employees only). 
    
    - If the file that you upload is unchanged from the previous one, you will receive a message that there's nothing new to upload.
        

### Elastic Cloud Billing

To publish content to [https://www.elastic.co/docs/api/doc/cloud-billing](https://www.elastic.co/docs/api/doc/cloud-billing) :

1. Check out the appropriate branch in the [cloud repo](https://github.com/elastic/cloud) (i.e. "master").
    
2. Get someone with Bump.sh admin or maintainer authority (or ask on #next-api-reference) to upload and release the following file: [https://github.com/elastic/cloud/blob/master/python-services-v3/openapi/billing-service.external.yaml](https://github.com/elastic/cloud/blob/master/python-services-v3/openapi/billing-service.external.yaml). For admin Bump.sh users, refer to [Bump.sh API admin instructions](https://docs.google.com/document/d/1xP_n2AusBhJcLmmJeCZZkAztzeykvkZtJ6la2XHz7Aw/edit?tab=t.0) (Elastic employees only).
    

### Elastic Cloud Serverless

The content in [https://www.elastic.co/docs/api/doc/elastic-cloud-serverless](https://www.elastic.co/docs/api/doc/elastic-cloud-serverless) is automatically generated and deployed per [2024 - Serverless Api Spec publication](https://docs.google.com/presentation/d/1y5BRJ8SY41Rj-DEMcIxASEFXL-zuPqLsfwKsbzfAtVs/edit#slide=id.p13) (Elastic employees only)

The source files are in several repos, such as [https://github.com/elastic/project-api](https://github.com/elastic/project-api), [https://github.com/elastic/cluster-api](https://github.com/elastic/cluster-api), and [https://github.com/elastic/serverless-api-specification](https://github.com/elastic/serverless-api-specification). For example, the `info.description` can be edited in [https://github.com/elastic/serverless-api-specification/blob/main/templates/production/public-user-serverless-header.yml](https://github.com/elastic/serverless-api-specification/blob/main/templates/production/public-user-serverless-header.yml)

### Elasticsearch

For info about the structure of the Elasticsearch specifications, refer to [elasticsearch-specification/docs](https://github.com/elastic/elasticsearch-specification/tree/main/docs).

To learn how to set up your environment and contribute to Elasticsearch API documentation, refer to the [Elasticsearch quickstart](./quickstart.md).

#### Publish

To publish content to [https://www.elastic.co/docs/api/doc/elasticsearch-serverless](https://www.elastic.co/docs/api/doc/elasticsearch-serverless) and [https://www.elastic.co/docs/api/doc/elasticsearch](https://www.elastic.co/docs/api/doc/elasticsearch): 

1. Check out the appropriate branch in the [elasticsearch-specification repo](https://github.com/elastic/elasticsearch-specification) (e.g. "main", or the current v9 or v8 branch).
    
2. (Optional) Regenerate the elasticsearch specification and OpenAPI documents by using [Makefile](https://github.com/elastic/elasticsearch-specification/blob/main/Makefile) commands.  
    Refer to [readme](https://github.com/elastic/elasticsearch-specification?tab=readme-ov-file#how-to-generate-the-openapi-representation) for the definitive details.  
    The output files are generally up-to-date since there are automated tasks that re-generate them. 
    
    1. `make setup`
        
    2. `make generate`
        
3. Generate the docs-specific OpenAPI documents, apply overlays, and run lint checks:  
    
    :::{note}
    Unlike the Kibana process, these files are not checked into the repo so you must do this task manually before uploading the files to Bump.sh.
    :::
    
    1. `make transform-to-openapi-for-docs` (new command added for 8.18+)
        
    2. `make overlay-docs`  
        The output files are: 
        
        1. Elasticsearch: `output/openapi/elasticsearch-openapi-docs-final.json` (new output file names as of 8.18+)
            
        2. Elasticsearch Serverless: `output/openapi/elasticsearch-serverless-openapi-docs-final.json`
            
    3. Run `make lint-docs`  
        If there are errors, they should be addressed before deployment. Warnings and info messages can be ignored.
        
        1. There is an error in the Elasticsearch document about two paths being ambiguous, which is true but also not something that needs to be fixed. It can be ignored for now.
            
4. Get assistance from someone with admin authority in Bump.sh (reach out on #next-api-reference Slack channel) to deploy the new output file as described in [https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/](https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/) and [Bump.sh API admin instructions](https://docs.google.com/document/d/1xP_n2AusBhJcLmmJeCZZkAztzeykvkZtJ6la2XHz7Aw/edit?tab=t.0) (Elastic employees only). 
    
    :::{important}
    You must ensure that you upload the file to the appropriate version in Bump.sh ("main", "v9", or "v8") as described in [https://docs.bump.sh/help/publish-documentation/branching/#deploy-to-a-specific-branch](https://docs.bump.sh/help/publish-documentation/branching/#deploy-to-a-specific-branch)
    :::
    
    - If the file that you upload is unchanged from the previous one, you will receive a message that there's nothing new to deploy.
        

### Kibana 

For info about how the OpenAPI documents are generated, refer to [kibana/oas_docs/readme](https://github.com/elastic/kibana/tree/main/oas_docs#readme).

#### Review

##### Review generated APIs

1. If you're reviewing Kibana APIs generated from code (per [Generating OAS for HTTP APIs](https://docs.elastic.dev/kibana-dev-docs/genereating-oas-for-http-apis)), check for:
        
        - API-level details:
            

            - **tags**: ['oas-tag: my tag'], which defines which group the API will belong to. Output as tag.
                
            - **availability**: { since '9.1.0', stability: 'experimental'}, which defines the lifecycle for each API. Output as `x-state`. 
                
            - **security**: { authz: { requiredPrivileges: ...}, which lists the specific Kibana security privileges required. Output in operation description.
                
            - **summary**, which is output as the [operation summary](https://elasticco.atlassian.net/wiki/spaces/DOC/pages/450494532/API+reference+docs#Summaries) (Elastic employees only).
                
            - **description**, which is output as the operation [description](https://elasticco.atlassian.net/wiki/spaces/DOC/pages/450494532/API+reference+docs#Descriptions) (Elastic employees only).            - Request and response examples, per the "Adding examples" section under [Route definitions](https://docs.elastic.dev/kibana-dev-docs/genereating-oas-for-http-apis#2-route-definitions).
                
        - **Parameter- and property-level details**:
            
            - Descriptions for all properties and parameters. For style guidance, refer to [Descriptions](https://elasticco.atlassian.net/wiki/spaces/DOC/pages/450494532/API+reference+docs#Descriptions) (Elastic employees only). 

##### Review OpenAPI files for individual features
  
If you're reviewing Kibana APIs that are derived from OpenAPI files for individual features, check for:
        
        - the general OpenAPI [documentation requirements](https://elasticco.atlassian.net/wiki/spaces/DOC/pages/450494532/API+reference+docs#Documentation-requirements)
            
        - the presence of x-state with API lifecycle details, per [Specification extensions](https://elasticco.atlassian.net/wiki/spaces/DOC/pages/450494532/API+reference+docs#Specification-extensions). 
            
2. If you're suggesting changes in a pull request, make your suggestions against the source files (e.g. in the typescript or feature-specific yaml files instead of the oas_docs folder).
    
3. If you notice that an entire API is being removed, it means that there will potentially be broken links to that page (either from docs or UIs or other places that link to APIs or in the SEO results). There are discussions about how to automatically take action when this occurs, but for now, reach out in #docs and we'll likely need to add a redirect in [https://github.com/elastic/docs-infra/blob/main/modules/elastic-docs-v3/api-redirects.json](https://github.com/elastic/docs-infra/blob/main/modules/elastic-docs-v3/api-redirects.json)
    

Refer to [oas_doccs/scripts](https://github.com/elastic/kibana/tree/main/oas_docs/scripts) for the list of individual files that are merged into the final Kibana API documents.

#### Publish

Followed these steps to publish content to [https://www.elastic.co/docs/api/doc/kibana](https://www.elastic.co/docs/api/doc/kibana) or [https://www.elastic.co/docs/api/doc/serverless](https://www.elastic.co/docs/api/doc/serverless): 

- Option 1: Publish the content that was automatically deployed (this should be the default path)
    
    - Just ask from someone with admin authority in Bump.sh (reach out `#docs` Slack channel) to deploy the latest documents.
        
        - Why so easy? There is a buildkite script ([publish_oas_docs.sh](https://github.com/elastic/kibana/blob/main/.buildkite/scripts/steps/openapi_publishing/publish_oas_docs.sh)) that deploys the OpenAPI documents to Bump.sh when a PR that affects those files is merged. This final step to release them is required because we've still got [Manual release mode](https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/#manually-release-a-deployment) enabled but that will eventually be turned off too. 
            
        - Note it's not yet completely foolproof because when a new version branch is forked or the "current" branch changes, the buildkite script needs updates. For an example of how to update that file, check out: [https://github.com/elastic/kibana/pull/219864](https://github.com/elastic/kibana/pull/219864)  
            
- Option 2: Generate and deploy the content manually (shouldn't be necessary any longer)
    
    1. Check out the appropriate branch in the [kibana repo](https://github.com/elastic/kibana) (e.g. "main" or the current v8 or v9 branch).
        
    2. Generate the output by using [Makefile](https://github.com/elastic/kibana/blob/main/oas_docs/makefile) commands:
        
        1. Go to the `oas_docs` folder.
            
        2. (Optional) Regenerate the OpenAPI content that is derived from the code. This generally shouldn't be necessary but if you need to force it to occur, refer to the commands in [https://github.com/elastic/kibana/blob/main/.buildkite/scripts/steps/checks/capture_oas_snapshot.sh](https://github.com/elastic/kibana/blob/main/.buildkite/scripts/steps/checks/capture_oas_snapshot.sh)  
            If you get questions from dev teams about the source files or process for adding new APIs, refer to:
            
            1. [https://docs.elastic.dev/kibana-dev-docs/genereating-oas-for-http-apis](https://docs.elastic.dev/kibana-dev-docs/genereating-oas-for-http-apis) in general
                
            2. [https://github.com/elastic/kibana/blob/main/x-pack/solutions/security/plugins/security_solution/docs/openapi/README.md](https://github.com/elastic/kibana/blob/main/x-pack/solutions/security/plugins/security_solution/docs/openapi/README.md) for Security APIs
                
        3. Run `make api-docs` (bundles all the disparate source files and applies overlays).
            
            1. For Kibana, the output file is `oas_docs/output/kibana.yaml`
                
            2. For Kibana Serverless, the output file is `oas_docs/output/kibana.serverless.yaml`
                
            3. If you get errors, it might mean you need to refresh the cache and/or bootstrap Kibana per [https://www.elastic.co/guide/en/kibana/current/development-getting-started.html#_install_dependencies](https://www.elastic.co/guide/en/kibana/current/development-getting-started.html#_install_dependencies)
                
        4. Run `make api-docs-lint`  
            If there are errors, they should be addressed before deployment. Warnings and info messages can be ignored.
            
    3. Preview the output in Bump.sh. After you [install the Bump.sh CLI](https://docs.bump.sh/help/continuous-integration/cli/#installation) you can generate a preview from the command line. For example: bump preview output/kibana.yaml
        
    4. Get assistance from someone with admin authority in Bump.sh (reach out on #next-api-reference Slack channel) to deploy the new output file as described in [https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/](https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/) and [Bump.sh API admin instructions](https://docs.google.com/document/d/1xP_n2AusBhJcLmmJeCZZkAztzeykvkZtJ6la2XHz7Aw/edit?tab=t.0) (Elastic employees only). 
        
        :::{important}
        You must ensure that you upload the file to the appropriate version in Bump.sh ("main", "v9", or "v8") as described in [https://docs.bump.sh/help/publish-documentation/branching/#deploy-to-a-specific-branch](https://docs.bump.sh/help/publish-documentation/branching/#deploy-to-a-specific-branch)

        If the file that you upload is unchanged from whatever was last automatically deployed, you will receive a message that there's nothing new to upload and should just release the latest existing file (for the appropriate branch).
        :::
        
            

### Logstash 

To publish content to [https://www.elastic.co/docs/api/doc/logstash](https://www.elastic.co/docs/api/doc/logstash) : 

1. Check out the appropriate branch in the [logstash repo](https://github.com/elastic/logstash/tree/main) (e.g. "main" ).
    
2. Find the OpenAPI document in the `docs/static/spec/openapi` folder. For example: [https://github.com/elastic/logstash/blob/main/docs/static/spec/openapi/logstash-api.yaml](https://github.com/elastic/logstash/blob/main/docs/static/spec/openapi/logstash-api.yaml)
    
3. Optionally, lint and preview the document. For example, install [redocly](https://redocly.com/docs/cli/installation) and [bump.sh](https://docs.bump.sh/help/continuous-integration/cli/) CLI, then run:
    
    1. `redocly lint logstash-api.yaml --config redocly.yaml`
        
    2. `bump preview logstash-api.yaml`
        
4. Get assistance from someone with admin authority in Bump.sh (reach out on #next-api-reference Slack channel) to deploy the new output file as described in [https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/](https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/) and [Bump.sh API admin instructions](https://docs.google.com/document/d/1xP_n2AusBhJcLmmJeCZZkAztzeykvkZtJ6la2XHz7Aw/edit?tab=t.0) (Elastic employees only). 
    
    - If the file that you upload is unchanged from the previous one, you will receive a message that there's nothing new to deploy.  
        

### Observability Intake (serverless)

To publish content to [https://www.elastic.co/docs/api/doc/observability-serverless](https://www.elastic.co/docs/api/doc/observability-serverless)

1. Check out the main branch in the [apm-managed-service repo](https://github.com/elastic/apm-managed-service).
    
2. Generate the output by using [Makefile](https://github.com/elastic/apm-managed-service/blob/main/Makefile) commands:
    
    1. Go to the root folder.
        
    2. Run `make api-docs` (bundles all the manually-maintained source files).  
        The output file is `docs/spec/openapi/bundled-apm-mis-openapi.yaml`.
        
    3. Run `make api-docs-lint`  
        If there are errors, they should be addressed before deployment. Warnings and info messages can be ignored.
        
3. Get assistance from someone with admin authority in Bump.sh (reach out on #next-api-reference Slack channel) to deploy the new output file as described in [https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/](https://docs.bump.sh/help/publish-documentation/deploy-and-release-management/) and [Bump.sh API admin instructions](https://docs.google.com/document/d/1xP_n2AusBhJcLmmJeCZZkAztzeykvkZtJ6la2XHz7Aw/edit?tab=t.0) (Elastic employees only). 
    
    - If the file that you upload is unchanged from the previous one, you will receive a message that there's nothing new to deploy.
