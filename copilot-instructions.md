Repository coding instruction

Rule
- Always wrap single-line if statements and loop bodies in curly braces ({ }).
- Applies to: if, else if, else (when containing a single statement) and loop constructs: for, foreach, while, do/while.

Examples
- Incorrect:
  if (condition) DoSomething();
  for (int i = 0; i < n; i++) DoWork(i);

- Correct:
  if (condition) { DoSomething(); }
  for (int i = 0; i < n; i++) { DoWork(i); }

Scope
- This guideline applies to all C# source files in this repository. Follow it when generating, editing, or reviewing code.

Rationale
- Always using braces avoids bugs when later adding statements and improves readability.

Rule
- Methods should be documented with XML comments, including a brief summary and parameter descriptions.
- Spellings should always be in British English.

Rule
- A datetime column stores UTC only if its name ends in "Utc" (e.g. CreatedAtUtc, ExpiryDateTimeUtc). Every other datetime column stores UK local wall-clock time, matching how MatchDateTime is stored and compared throughout the app.
- Write UK wall-clock values with UkClock.Now (Application/Common/UkClock.cs), never DateTime.Now - the app is hosted on shared IIS whose server timezone is not guaranteed to be UK. Use DateTime.UtcNow only for a column whose name ends in "Utc".
- Name any new column accordingly, and give it a matching default: getdate() for wall-clock, sysutcdatetime() for a "Utc" column.

Scope
- This guideline applies to all C# source files and to the SSDT schema under Database/.

Rationale
- The two kinds of column are indistinguishable at the point of use, so the name has to carry it. Mixing them silently shifts values by an hour through BST, which corrupts comparisons between columns as well as displayed times.
