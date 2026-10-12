# DEV-80 - Assumptions (taste, Patron)

- A1 [assumed] Optimistic Pending row title is the last segment of the submitted folder path; the 2s poll replaces it with the server title.
- A2 [assumed] Labels: button 'Import folder'; dialog title 'Import a movie folder'; input label 'Folder path'; submit 'Import'; cancel 'Cancel'; row action 'Retry'.
- A3 [assumed] Status badge text is the enum name as received (`Pending`, `Enriched`, `NotFound` shown as 'Not found', `Failed`).
- A4 [assumed] Empty library copy: 'No movies yet. Import a folder to get started.'
- A5 [assumed] List order is the server browse order; the client does not re-sort after a poll.
