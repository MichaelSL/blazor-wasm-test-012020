# Blazor-WASM-Regex

A Blazor Web App for testing .NET (C#) regular expressions in your browser. The regex engine runs client-side in WebAssembly, so your patterns and test data never leave the browser.

## DEMO

Now hosted: [dotnet-regex](https://dotnet-regex.com/)

As all work is done in the browser the app is self-hosted on opi zero device with extremely low resources as an experiment. To keep the server's work minimal, the pages are rendered to static HTML when the Docker image is built, and the server only serves files. Only the regex tester itself is an interactive WebAssembly component.

## Application Usage

### Features

- **Live regex testing** with the real C# regex engine (runs entirely client-side in WebAssembly): results update as you type
- **Two-pane workbench**: pattern, options, replacement and test strings on the left, results on the right (stacked on phones)
- **Highlighting everywhere**: the pattern is syntax-colored, matches are highlighted inside the test strings, and each capture group has its own color in the pattern, the matches and the Groups view. Hover a match to highlight it in every view
- **Per-line gutter** in the test editor: how many matches each line has
- **Regex options**: Ignore Case, Multiline and Singleline, plus an "Advanced" section with AnyNewLine (new in .NET 11), NonBacktracking, Explicit Capture, Ignore Pattern Whitespace, Right To Left, Culture Invariant and ECMAScript. Options that cannot be combined are disabled with the reason
- **Clear errors**: an invalid pattern is underlined at the failing character with the .NET parse error; a timeout (2-second limit against catastrophic backtracking) has its own amber state
- **Sample patterns** with named groups (Email, Phone, URL)
- **Replace** with `$1` / `${name}` substitutions, shown as a before/after diff
- **Copy buttons** on match, group, split and replace values
- **Export** matches as JSON, CSV or plain text
- **Local storage persistence** - your pattern, tests, options and the Live setting are saved between sessions
- **Keyboard shortcut**: Ctrl+Enter (Cmd+Enter on macOS) runs the test
- **.NET regex quick reference** below the tester; click a token to insert it at the cursor
- **Light and dark themes** (follows the system setting until you pick one)

### Basic Usage

#### 1. Testing a Regex Pattern

1. **Enter a regex pattern** in the "Regular expression" field
   - Example: `\d+` (matches one or more digits)
   - The active options are shown after the pattern, e.g. `/\d+/ i`

2. **Configure options** with the chips under the pattern:
   - **Ignore case**: Makes pattern case-insensitive
   - **Multiline**: `^` and `$` match line starts/ends
   - **Singleline**: `.` matches newline characters
   - Click **Advanced** for the less common options (hover an option for a short explanation):
     - **Any NewLine** (.NET 11): `^`, `$`, `\Z` and `.` treat `\r\n`, `\r`, `\n`, `\u0085`, `\u2028` and `\u2029` as line breaks. Each line of the test strings is matched on its own, so in the tester this mostly matters for `\u0085`, `\u2028` or `\u2029` inside a line
     - **NonBacktracking**: linear-time engine; no backreferences, lookarounds, atomic groups, conditionals, balancing groups or `\G`
     - **Explicit capture**: only named/numbered groups capture
     - **Ignore pattern whitespace**: unescaped whitespace in the pattern is ignored and `#` starts a comment
     - **Right to left**: search from the end of the input (highlighting stays in input order; the Table, Groups and exports list matches right to left)
     - **Culture invariant**: ignore cultural differences when ignoring case
     - **ECMAScript**: ECMAScript-compliant behaviour
   - Options that .NET cannot combine are disabled, with the reason: ECMAScript only works with Ignore case, Multiline and Culture invariant; NonBacktracking can't be combined with Right to left, ECMAScript or Any NewLine

3. **Enter test strings** in the "Test strings" editor, one per line. Each line is matched on its own (lines are no longer trimmed or de-duplicated):
   ```
   test123
   abc456def
   no numbers here
   ```

4. With **Live** on (the default), results update as you type. Turn Live off to run only when you click **Test** or press Ctrl+Enter
   - The status bar above the results shows the match count, how many lines matched, the number of capture groups and the time taken

#### 2. Using Sample Patterns

Click **Email**, **Phone** or **URL** next to the pattern to load a sample. The test strings are replaced only when they are empty or still hold another sample's data.

- **Email**: `(?<user>[\w.+-]+)@(?<domain>[\w-]+\.[\w.]+)`
- **Phone**: `(?<country>\+\d{1,3})?[\s.-]?\(?(?<area>\d{3})\)?[\s.-]?(?<exchange>\d{3})[\s.-]?(?<line>\d{4})`
- **URL**: `(?<scheme>https?)://(?<host>[\w.-]+)(?<path>/[^\s?#]*)?(?:\?(?<query>[^\s#]*))?`

On the first visit the Email sample is loaded so there is something to look at.

#### 3. Viewing Results

The result tabs show their counts, e.g. **Matches 3**:

- **Matches**: every test line with its matches highlighted (colored by capture group), and a matched / no match marker per line
- **Split**: `Regex.Split` of each line, one chip per part. .NET includes captured groups in the result
- **Table**: index, length and value of every match, per line
- **Groups**: per line and match, every group with its number, name, value, index and length. Shows how to write `(?<name>...)` when the pattern has no groups. Example:
  ```
  Pattern: (?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})
  Test: 2025-12-02

  Match 1  2025-12-02  index 0 · length 10
    0 (match)  2025-12-02  0  10
    year #1    2025        0   4
    month #2   12          5   2
    day #3     02          8   2
  ```
- **Replace**: the original line (matches struck through) and the replaced line (inserted text highlighted)

#### 4. Replace Functionality

Replace matched text using capture groups:

1. **Enter a replacement pattern** in the "Replacement" field
   - Use `$1`, `$2`, etc. for numbered capture groups
   - Use `${name}` for named capture groups
   - Use literal text for static replacements

2. The **Replace** tab updates live; the **Replace** button runs the test and switches to that tab

**Examples:**

- **Date format conversion:**
  ```
  Pattern: (\d{4})-(\d{2})-(\d{2})
  Replacement: $2/$3/$1
  Test: 2025-12-02
  Result: 12/02/2025
  ```

- **Named groups:**
  ```
  Pattern: (?<first>\w+)\s(?<last>\w+)
  Replacement: ${last}, ${first}
  Test: John Doe
  Result: Doe, John
  ```

- **Adding text:**
  ```
  Pattern: \d+
  Replacement: Number: $0
  Test: 123
  Result: Number: 123
  ```

#### 5. Exporting Results

Use the **Export** menu (enabled when there are matches) to download the matches in one of three formats. A test line that appears more than once is exported once in JSON:

##### **JSON Export**
- Structured data with full match details
- Includes capture groups, indices, and lengths
- Format:
  ```json
  {
    "test123": [
      {
        "Value": "123",
        "Index": 4,
        "Length": 3,
        "Groups": []
      }
    ]
  }
  ```

##### **CSV Export**
- Spreadsheet-compatible format
- Columns: Test String, Match Value, Match Index, Match Length
- Easy to import into Excel, Google Sheets, etc.

##### **Text Export**
- Human-readable plain text report
- Includes:
  - Regex pattern used
  - Total match count
  - Per-string match breakdown
- Format:
  ```
  Regex Pattern: \d+
  Total Matches: 2
  Test Strings with Matches: 2

  Test String: test123
  Matches (1):
    - Value: "123" (Index: 4, Length: 3)
  ```

### Advanced Features

#### Local Storage Persistence
Your regex patterns, test strings, and options are automatically saved to browser local storage and restored when you return to the application.

#### Performance Monitoring
Every test execution displays timing information in milliseconds, helping you identify performance issues with complex patterns.

### Common Use Cases

#### 1. Email Validation
```
Pattern: ^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$
Options: None
Test:
  user@example.com
  invalid.email
  test@domain.co.uk
```

#### 2. Phone Number Extraction
```
Pattern: \(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}
Options: None
Test:
  Call me at (555) 123-4567 or 555-987-6543
```

#### 3. Log File Parsing
```
Pattern: (?<timestamp>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\s(?<level>\w+)\s(?<message>.+)
Options: Multiline
Test:
  2025-12-02 14:30:45 ERROR Connection failed
  2025-12-02 14:30:46 INFO Retrying connection
```

#### 4. URL Parameter Extraction
```
Pattern: [?&](?<param>\w+)=(?<value>[^&]+)
Options: None
Test:
  https://example.com?user=john&id=123&token=abc
```

#### 5. Code Comment Removal
```
Pattern: //.*$
Options: Multiline
Replace: (empty)
Test:
  var x = 5; // This is a comment
  var y = 10; // Another comment
Result:
  var x = 5; 
  var y = 10; 
```

### Troubleshooting

**Pattern doesn't match:**
- Check if you need IgnoreCase option
- Verify escape sequences: use `\\` for literal backslash
- Test subpatterns individually

**Expression times out:**
- Simplify nested quantifiers: `(a+)+` is problematic
- Use specific quantifiers: `{1,10}` instead of `+`
- Avoid alternation with overlapping patterns

**No results displayed:**
- Ensure "Test" button is clicked after pattern changes
- Check browser console (F12) for errors
- Verify test strings contain expected content

**Export buttons disabled:**
- Export buttons activate only when matches exist
- Click "Test" first to generate matches
- Verify pattern actually matches test strings
