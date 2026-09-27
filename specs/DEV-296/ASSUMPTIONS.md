# DEV-296 Patron assumptions

- [assumed] Log successful completion at Information with request type and elapsed milliseconds; log validation failure at Warning and other exceptions at Error, then rethrow. Message text should be concise and contain no request payload. Basis: DEV-296 ticket note line 15; constitution lines 164-165, 193-210; specs/PRODUCT.md lines 26-30.
