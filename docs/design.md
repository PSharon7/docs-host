# Docs Hosting Design Spec

This is the design spec for cosmos db dhs

## Summary
- Use URL as the partition key
- Locale fallback and branch fallback are supported in DHS service
- No bloom filter anymore, one query fron rendering will always get 1 document or 404
- Git version controlling doesn't seem to work, the output are not only related to the git version, also including configuration version, pipeline behavior version

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
| latest_hash   |                                                  |                                   |
| page_id       |                                                  |                                   |

## Page Table

| field name    | description                                         | note                              |
|---------------|-----------------------------------------------------|-----------------------------------|
|      id       | The auto generated id                               |                                   |
| latest_hash   | The output content hash                             |                                   |
| page_metadata |                                                     |                                   |
| page_content  |                                                     |                                   |


## Workflow

- Publish
  - List all documents based on `document table` (docset_name + branch + locale)
  - Upload missing page content/metadata -> page_id
  - Insert missing document (url + last_hash + page_id)
  - Delete un-used document(DHS will auto clean corresponding page table)

- Query
  - Get document from document table (version branch + locale + url)
  - Locale fallback and branch fallback
  - Priority
  - Return 1 document with page_id or 404
