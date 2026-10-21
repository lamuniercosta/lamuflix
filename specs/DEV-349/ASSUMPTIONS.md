# DEV-349 assumptions

- [assumed] Q3 Patron approved the category name `OllamaIntegration` and opt-in process variable `LAMUFLIX_RUN_OLLAMA_TESTS=1`. These names are taste choices; explicit isolation before resource initialization is required by DEV-349:22 and constitution 526-527. Full exchange: CONCLUSIONS.md Q3.
- [assumed] Q4/Q6 initializer service name: `ollama-model-init`; persistent development volume: `ollama-models`.
- [assumed] Q6 configurable operational defaults: server readiness 120 seconds, model pull 900 seconds, generation 120 seconds, polling 2 seconds. Cleanup receives its own allowance; no harness threshold changes are authorized.
- [assumed] Q5/Q6 fixed sample plot: `A botanist follows a signal from an abandoned observatory and discovers a hidden garden.`
- [assumed] Q5/Q6 test name: `OllamaEmbeddingIntegrationTests.GenerateAsync_SamplePlot_Returns384DimensionalEmbedding`.
