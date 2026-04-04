---
description: "Save a note or rule to workspace memory so all agents remember it. Usage: /remember <what to remember>"
argument-hint: "Note or rule to store in agent memory"
agent: "agent"
tools: [editFiles]
---

Add the following note to the workspace memory file [memory.instructions.md](../instructions/memory.instructions.md):

```
$input
```

## Instructions

1. Read the current contents of [memory.instructions.md](../instructions/memory.instructions.md).
2. Append the note under a relevant heading. If a matching heading already exists, add to it. If not, create a new heading.
3. Keep entries short — one bullet per fact, preference, or rule.
4. Do not alter the YAML frontmatter.
5. Do not remove any existing content.
6. Avoid adding duplicate bullets; if a near-duplicate exists, refine the existing bullet instead of adding a new one.
7. If the memory file grows beyond ~200 lines, move stale notes into `.github/instructions/archive/memory.archive.md` and keep active rules in the main file.

## Format for appended content

```markdown
## <Topic Heading>

- <concise note>
```

Choose the heading based on the note's topic (e.g., `## Preferences`, `## Conventions`, `## Project Rules`, `## Do Not`).
If the note clearly belongs under an existing heading, append it there instead of creating a new one.
