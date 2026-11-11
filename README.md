# Blazor-WASM-Regex

A Blazor Web App for testing .NET (C#) regular expressions in your browser. The regex engine runs client-side in WebAssembly, so your patterns and test data never leave the browser.

## DEMO

Now hosted: [dotnet-regex](https://dotnet-regex.com/)

As all work is done in the browser the app is self-hosted on opi zero device with extremely low resources as an experiment. To keep the server's work minimal, the pages are rendered to static HTML when the Docker image is built, and the server only serves files. Only the regex tester itself is an interactive WebAssembly component.

## Application Usage

### Features

The Blazor WASM Regex Tester provides comprehensive regex testing capabilities directly in your browser:

- **Real-time regex testing** with C# regex engine (runs entirely client-side in WebAssembly)
- **Regex options**: Ignore Case, Multiline and Singleline, plus an "Advanced" section with AnyNewLine (new in .NET 11), NonBacktracking, Explicit Capture, Ignore Pattern Whitespace, Right To Left, Culture Invariant and ECMAScript
- **Sample patterns** for common use cases (Email, Phone, URL)
- **Capture group visualization** for named and numbered groups
- **Replace functionality** with capture group support
- **Export capabilities** (JSON, CSV, plain text formats)
- **Local storage persistence** - your patterns and tests are saved between sessions
- **Timeout protection** (2-second limit) prevents catastrophic backtracking
- **Keyboard shortcut**: Ctrl+Enter (Cmd+Enter on macOS) runs the test
- **.NET regex quick reference** below the tester: character classes, anchors, quantifiers, groups and lookarounds, inline options, substitutions and escapes

### Basic Usage

#### 1. Testing a Regex Pattern

1. **Enter a regex pattern** in the "Regex:" field
   - Example: `\d+` (matches one or more digits)
   
2. **Configure options** using checkboxes:
   - **Ignore Case**: Makes pattern case-insensitive
   - **Multiline**: `^` and `$` match line starts/ends
   - **Singleline**: `.` matches newline characters
   - Click **Advanced** for the less common options (hover a checkbox for a short explanation):
     - **Any NewLine** (.NET 11): `^`, `$`, `\Z` and `.` treat `\r\n`, `\r`, `\n`, `\u0085`, `\u2028` and `\u2029` as line breaks. Each line of the Tests box is matched on its own and trimmed, so in the tester this mostly matters for `\u0085`, `\u2028` or `\u2029` inside a line
     - **NonBacktracking**: linear-time engine; no backreferences, lookarounds, atomic groups, conditionals, balancing groups or `\G`
     - **Explicit Capture**: only named/numbered groups capture
     - **Ignore Pattern Whitespace**: unescaped whitespace in the pattern is ignored and `#` starts a comment
     - **Right To Left**: search from the end of the input (highlighting stays in input order; the Table, Capture Groups and exports list matches right to left)
     - **Culture Invariant**: ignore cultural differences when ignoring case
     - **ECMAScript**: ECMAScript-compliant behaviour
   - Some combinations are rejected by .NET and show a message instead of results: ECMAScript only works with Ignore Case, Multiline and Culture Invariant; NonBacktracking can't be combined with Right To Left, ECMAScript or Any NewLine

3. **Enter test strings** in the "Tests:" textarea (one per line)
   - Example:
     ```
     test123
     abc456def
     no numbers here
     ```

4. **Click "Test"** (or press Ctrl+Enter) to execute the regex
   - Results appear in the message area showing match count and execution time

#### 2. Using Sample Patterns

Select from the **Samples** dropdown to quickly load common patterns:

- **Email**: Validates email addresses
  ```
  Pattern: [a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}
  Test: user@example.com, invalid.email, test@domain.co.uk
  ```

- **Phone**: Matches US phone numbers
  ```
  Pattern: \(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}
  Test: (555) 123-4567, 555-123-4567, 5551234567
  ```

- **URL**: Validates web URLs
  ```
  Pattern: https?://[^\s]+
  Test: https://example.com, http://test.org/path
  ```

#### 3. Viewing Results

The application provides **five result tabs**:

##### **Matches Tab**
- Shows test strings with matched portions highlighted in yellow
- Only displays strings that have matches

##### **Split List Tab**
- Shows how the regex splits each test string
- Uses pin icons (📌) as delimiters between segments

##### **Table Tab**
- Detailed match information in tabular format
- Shows:
  - Test string
  - Match index (position in string)
  - Match length
  - Match value

##### **Capture Groups Tab**
- Displays captured groups for patterns with parentheses
- Shows both named groups: `(?<name>pattern)` and numbered groups: `(pattern)`
- Example:
  ```
  Pattern: (?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})
  Test: 2025-12-02
  
  Results:
  - Match 0 (Index: 0, Length: 10)
    - 0: "2025-12-02" (Index: 0, Length: 10)
    - year: "2025" (Index: 0, Length: 4)
    - month: "12" (Index: 5, Length: 2)
    - day: "02" (Index: 8, Length: 2)
  ```

##### **Replace Tab**
- Shows original strings and their replaced versions side-by-side
- Activated after using the Replace feature

#### 4. Replace Functionality

Replace matched text using capture groups:

1. **Enter replacement pattern** in "Replacement Pattern:" field
   - Use `$1`, `$2`, etc. for numbered capture groups
   - Use `${name}` for named capture groups
   - Use literal text for static replacements

2. **Click "Replace"** to apply replacements

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

Export match results in three formats:

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
