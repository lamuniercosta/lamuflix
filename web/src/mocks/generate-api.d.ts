declare module '*/generate-api.mjs' {
  export const SOURCE: URL;
  export const DEFAULT_TYPES_OUTPUT: URL;
  export const DEFAULT_HANDLERS_OUTPUT: URL;
  export function renderTypes(): Promise<string>;
  export function renderHandlers(): Promise<string>;
  export function generateTypes(outFile?: string | URL): Promise<void>;
  export function generateHandlers(outFile?: string | URL): Promise<void>;
}

declare module '*/check-api.mjs' {
  export function filesMatch(committed: Uint8Array | null, fresh: Uint8Array): boolean;
  export function checkGenerated(
    label: string,
    generate: (outFile: string | URL) => Promise<void>,
    committedPath: string | URL,
    tempFile: string,
  ): Promise<boolean>;
  export function main(): Promise<void>;
}
