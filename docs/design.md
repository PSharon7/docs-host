# Docs Hosting Design Spec

This is the design spec for cosmos db DHS

## Summary
- Use URL as the partition key of below `document` table
- Locale fallback and branch fallback are supported in DHS service
- No bloom filter anymore, one query from rendering will always get 1 document or 404
- Git version controlling doesn't seem to work, the output are not only related to the git version, also including configuration version, pipeline behavior version
- Split page metadata and page content to another table `page` and use hash to hash for referencing. 

## Document table

| field name    | description                                      | note                              |
|---------------|--------------------------------------------------|-----------------------------------|
| url           | host name + base path + relative path            | partition key                     |
| base path     |                                                  |                                   |
| relative path |                                                  |                                   |
| locale        |                                                  |                                   |
| branch        |                                                  |                                   |
| version       |                                                  |                                   |
| docset name   |                                                  |                                   |
| page_hash     | The output hash of page content/metadata         |                                   |
| page_id       | The id of page table                             |                                   |

## Page Table

| field name    | description                                         | note                              |
|---------------|-----------------------------------------------------|-----------------------------------|
| id            | The auto generated id                               |                                   |
| hash          | The output content hash                             |                                   |
| page_metadata |                                                     |                                   |
| page_content  |                                                     |                                   |


## Workflow

- Publish
  - Upload missing page content/metadata -> auto generated -> `page_id`
  - Upload all documents with `page_hash` + `page_id` and latest commit flag
  - Switch latest commit flag
  - DHS clean un-used documents and corresponding pages
  
- Query
  - Get documents from document table (version branch + locale + url)
  - Locale fallback and branch fallback
  - Priority
  - Return 1 document with page_url(cosmos url with `page_id`) or 404
