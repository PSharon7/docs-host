# Docs Hosting Design Spec
## Document table
- Partition Key: url(host + base path + relative path)

| field name | description | note |
|------------|-------------|------|
| url  |             |      |
| base path  |             |      |
| relative path |          |      |
| locale     |             |      |
| branch     |             |      |
| commit     |             |      |
| version    |             |      |
| docset name |            |      |

## Commit Table

| field name | description | note |
|------------|-------------|------|
| url        |             |      |
| locale     |             |      |
| branch     |             |      |
| commit     |             |      |
| docset name |            |      |

## Workflow

### Publish
- New document:
  - Query Url + locale + commit1  
  - Not Found  
  - Insert Document table(commit1) and commit table  
  - Commit table:  
  - Url + locale + branch1 + commit1  
- Update document:
  - Query Url + locale + commit2
  - Not Found
  - Insert document table(commit2) and inset commit table
  - Commit table:
    - Url + locale + branch1 + commit2
    - Url + locale + branch1 + commit1
- Fork branch2:
  - Query url + locale + commit2
  - Found
  - Insert commit table
  - Commit table:
     - Url + locale + branch1 + commit2
  	- Url + locale + branch1 + commit1
  	- Url + locale + branch2 + commit2

### Query(URL + branch1 + locale)
- Query commit table
- Found commit2 + commit 1
- Query doucment table Url  + commit2
- Found 1 document
