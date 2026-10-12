# PDF highlights

Select text within one page and choose “Đánh dấu” to add a yellow highlight. Multiple selected lines are stored as separate normalized rectangles, so zoom and page rerenders preserve placement. Cross-page selection remains available to AI but highlighting requires one page at a time.

Use the highlighter icon or sidebar “Đánh dấu” button to list marks, jump to their page or delete them. Marks are overlays in the reader and do not modify the original PDF bytes.

Guest marks live only in memory and disappear after reload/document changes. Account marks are included in the private document reading state, autosaved and restored with PDF/chat/AI history. Wait for the saved status before reload. Legacy documents without highlights default to an empty list.

Limits: 200 highlights/document, 5,000 text characters/highlight, 200 rectangles/highlight. The API validates page numbers, coordinate ranges and bounds, and uses existing owner/session checks. Both FE and BE must be deployed to persist the new state field.
