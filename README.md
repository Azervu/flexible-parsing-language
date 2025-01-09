

# Flexible Parsing Language


| Operator | Description                            |
|:---------|:---------------------------------------|
| `.`      | Operator Separator / Read              |
| `:`      | Write                                  |
| `{` `}`  | Branching                              |
| `(` `)`  | Grouping                               |
| `,`      | Parameter Separator                    |
| `*`      | Foreach                                |
| `:*`     | Write Foreach                          |
| `~`      | Name                                   |
| `|`      | Function call                          |
| `#`      | Lookup                                 |
| `##`     | Change Lookup Context                  |
| `'`      | Escape                                 |
| `"`      | Escape                                 |
| `\`      | Un Escape                              |
| `@`      | Context at start of current group      |
| `$`      | Root                                   |
| `:$`     | Write Root                             |


## function example

payload
['apple', 'troll', 'pear', 'bear']

query
|json*|regex('apple|bear')

result
['apple','bear']