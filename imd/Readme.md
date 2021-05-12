# imd - Inline Markdown

imd is a tool that watches the file system for changes of *Inline Markdown Files* (*.i.md) and creates the respective Markdown (*.md) and image files (*.png) from it.

An *Inline Markdown File* is a markdown file that contains inline blocks delimited by `@start[xxx]` and `@end[xxx]` that contain input for other tools.

Example:
````
# Sequence Diagram

@startuml
A --> B
@enduml
````

Currrently supported tools:
* Plantuml (`@startuml`, `@enduml`)

Planned:
* Powershell (`@startposh`, `@endposh`)
