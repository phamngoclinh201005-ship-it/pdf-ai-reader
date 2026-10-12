# Reading tools and spatial library

## Reader

Open **Ghi chú / Tìm / Bookmark** in the left sidebar (or the highlight icon in the header).

- Select text on one page and choose **Đánh dấu**. The Notes tab lets you attach up to 2,000 characters to each highlight.
- Bookmarks save the current page with an optional name (120 characters). Maximum 200 per document; duplicate pages are rejected. Select a bookmark to return to its page.
- Search uses the PDF text already extracted on the device, with a 180 ms debounce, case-insensitive matching and up to 100 occurrences. Results link to the source page. Scan-only PDFs need OCR; partially extracted documents show a notice. No AI calls.
- Outline uses the PDF's native internal destinations, up to 200 entries / 8 nesting levels. External links and malformed destinations are skipped. No outline is fabricated if the file has none.
- Export downloads a Markdown file with highlights, personal notes, bookmarks and page references. It does not upload anything.

Member notes/bookmarks are saved in the document's existing `ReadingState` in MongoDB; the same owner checks, limits and debounced persistence apply. Old states without these fields load with empty arrays. Guests retain them only while the reader is mounted. Reloading loses guest data.

## Three.js / 360° / VR

Open **Thư viện 3D / 360°** from the reader or library, route `/explore`.

- Three.js renders a procedural scene with books named after the authenticated user's PDFs. Click a book, then **Mở tài liệu** to resume in the normal PDF reader.
- Desktop mouse / touch orbit and zoom work without a headset. An accessible list below the scene includes every document; the scene shows the first 24 to bound GPU work.
- Local JPG/PNG/WebP panoramas, max 12 MB and approximately 2:1, can replace the environment. They are resized to max 4,096 pixels / GPU texture limit and remain on-device. No external panorama, model CDN, account or API key is required.
- A WebXR button appears only when the browser/device reports immersive VR support. Controller rays can select books. This is a spatial document launcher, not PDF page rendering inside VR.
- Scene resources, controls, textures, resize observers, render loop and XR session are released on exit. Rendering pauses when the tab is hidden outside an XR session. GPU pixel ratio is capped at 1.5. The normal document list remains available if WebGL fails.

Production Render HTTPS supports the secure-context prerequisite, but actual VR requires compatible browser/headset. Headset/controller behavior must be verified on physical hardware; desktop automation does not prove this.

Official implementation references: [OrbitControls](https://threejs.org/docs/pages/OrbitControls.html), [WebXR setup](https://threejs.org/manual/pages/webxr-basics.html).

## Release

Deploy both BE (new ReadingState fields) and FE. No new environment variables, paid providers or database migration commands. Additive fields preserve existing PDFs/auth/OTP/AI behavior.

Validation: FE unit tests cover search matches, bounds and Markdown export. Library integration tests verify note/bookmark round-trip, malformed bookmark rejection, note length, owner isolation and legacy library operations using a temporary MongoDB. Browser checks cover note/bookmark restore, search navigation, Markdown download, spatial selection, guest/member behavior and mobile layout. Production build/typecheck and AI smoke tests check the rest of the app.
