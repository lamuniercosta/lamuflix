# DEV-345 Assumptions

- `[assumed]` `FakeEmbeddingGenerator` derives each 384-element `float` vector deterministically from the UTF-8 input string via SHA-256 counter blocks and L2-normalizes the resulting vector to unit length (`1.0f`) so downstream cosine-distance tests have well-defined unit vectors (`specs/PRODUCT.md:28-29`, `DEV-345` Scope #2 & AC #1-#2).
- `[assumed]` `FakeEmbeddingGenerator` hashes exact UTF-8 input bytes with no case or whitespace normalization (`specs/PRODUCT.md:28-29`, `CONCLUSIONS.md` Q2).
- `[assumed]` `FakeEmbeddingGenerator` rejects `null` batch / `null` input elements and any non-null `options.Dimensions != 384`, while accepting empty batches and `""` (empty string) (`specs/PRODUCT.md:28-29`, `CONCLUSIONS.md` Q2).
