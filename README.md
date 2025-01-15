

# Flexible Parsing Language

FPL is intended as a robust and extensible parsing language that comes with XML and JSON support.
Supports custom read and write modules as well as conversion and filter functions
The syntaxt is similar to Json path where applicable

| Operator | Description                            |
|:---------|:---------------------------------------|
| `.`      | Operator Separator / Read              |
| `:`      | Write                                  |
| `{` `}`  | Write Branching Group Operator         |
| `(` `)`  | Parameter Grouping                     |
| `,`      | Group Separator                        |
| `*`      | Foreach                                |
| `:*`     | Write Foreach                          | 
| `~`      | Name                                   |
| `\|`      | Function call                          | 
| `#`      | Lookup                                 |
| `##`     | Change Lookup Context                  |
| `'` `"`  | Literal Operators                      | 
| `\`      | Escape Literal                         |
| `@`      | Context at start of current group      |
| `$`      | Set read cursor to Root                |
| `:$`     | Set write cursor to Root               |



new object[] { "Simple branch test", "{'k': {'ka': 'va', 'kb': 'vb'}}", "k{@ka:ha}kb:hb", "{'ha':'va','hb':'vb'}" },



## Foreach Example
### Payload
[[["a","b"],["c","d"]],[["e","f"],["g","h"]]]

| Query     | Result                                     |
|:----------|:-------------------------------------------|
| `***`     | ["a","b","c","d","e","f","g","h"]          |
| `*:***`   | [["a","b","c","d"],["e","f","g","h"]]      |
| `**:**`   | [["a","b"],["c","d"],["e","f"],["g","h"]]  |
| `**:*:h*` | [{"h":["a","b"]},{"h":["c","d"]},{"h":["e","f"]},{"h":["g","h"]}]  |


## Branching Example


### Payload
[{"k1":1, "k2": 11}, {"k1":2, "k2": 12}, {"k1":3, "k2": 13}]

### Query
\*:\*{@k1:h1}k2:h2

### Result
[{"h1":1,"h2":11},{"h1":2,"h2":12},{"h1":3,"h2":13}]

## Literal Example

### Payload
{"* *": ["a", {"2" : "b"}, "c"]}

### Query
"'* *'.1.'2'"

### Result
["b"]

## Escape Example

### Payload
{"> <": ["a", {"2" : "b"}, "c"]}

### Query
"'> <'.1.'2'"

### Result
["b"]

## Function Example

### Payload
"[\"<a>apple</a>\", \"<a>troll</a>\", \"<a>pear</a>\", \"<a>bear</a>\"]"

### Query
"|json*|regex('apple|bear')|xml.a"

### Result
["apple", "bear"]


## Root Example

### Payload
{"a": { "b": { "t28": "v" } }, "metadata": {"idkey": "t28"}}

### Query
"a.b($metadata.idkey)"

### Result
['v']

## Custom Converter Example

```c#

class DateTimeParser : IConverterFunction
{
    public string Name => "datetime";
    public object Convert(object value)
    {
        if (value is not string raw)
            raw = value.ToString();
        return DateTime.Parse(raw).ToUniversalTime();
    }
}

public DateTime ParseExample()
{

    var payload = "{\"data\":\"2024-01-15T20:11:17+01:00\"}";
    var query = $"|json.data|datetime";
    var compiler = new FplCompiler();
    compiler.RegisterConverter(new DateTimeParser());
    var parser = compiler.Compile(query);
    var result = parser.Parse(payload);

    return ((List<object>)result)[0];
}
```
