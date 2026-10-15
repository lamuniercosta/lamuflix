# DEV-80 - Assumptions (taste, Patron)

- A1 [assumed] Optimistic Pending row title is the last segment of the submitted folder path; the 2s poll replaces it with the server title.
- A2 [assumed] Labels: button 'Import folder'; dialog title 'Import a movie folder'; input label 'Folder path'; submit 'Import'; cancel 'Cancel'; row action 'Retry'.
- A3 [assumed] Status badge text is the enum name as received (`Pending`, `Enriched`, `NotFound` shown as 'Not found', `Failed`).
- A4 [assumed] Empty library copy: 'No movies yet. Import a folder to get started.'
- A5 [assumed] List order is the server browse order; the client does not re-sort after a poll.
- A6 [assumed] First-page browse request is `sort=Title&direction=Ascending&page=1&pageSize=20` (ND-02; Keel recommendation, recon-DEV-80:123-125). The client sends it as fixed constants. There are no sort or paging controls (brief.md:17), and A5 server order is kept.
- A7 [assumed] Retry 404/409 inline row error copy is `Something went wrong. Try again.` (same generic as import 400/network).
