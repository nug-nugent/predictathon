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
