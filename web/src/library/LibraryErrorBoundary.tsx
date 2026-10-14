import { Component, type ReactNode } from 'react';

export interface LibraryErrorBoundaryProps {
  children?: ReactNode;
  resetKey?: unknown;
  queryKey?: unknown;
  fallback?: ReactNode | ((error: unknown) => ReactNode);
  fallbackRender?: (error: unknown) => ReactNode;
  FallbackComponent?: (props: { error: unknown; reset: () => void }) => ReactNode;
  onRetry?: () => void;
  retry?: () => void;
  refetch?: () => void;
  onReset?: () => void;
  reset?: () => void;
  clearFilters?: () => void;
  clear?: () => void;
  onClearFilters?: () => void;
  onClear?: () => void;
  onResetFilters?: () => void;
  resetFilters?: () => void;
  className?: string;
}

interface LibraryErrorBoundaryState {
  error: unknown | null;
  resetKey: unknown;
}

function resolveResetKey(props: LibraryErrorBoundaryProps): unknown {
  return props.resetKey !== undefined ? props.resetKey : props.queryKey;
}

function isInvalidFilterError(error: unknown): boolean {
  if (!error || typeof error !== 'object') return false;
  const record = error as Record<string, unknown>;
  const status =
    (record as { status?: unknown }).status ??
    (record.response as { status?: unknown } | undefined)?.status ??
    (record.problem as { status?: unknown } | undefined)?.status;
  if (status === 422) return true;
  if (record.invalidFilter === true) return true;
  return false;
}

export class LibraryErrorBoundary extends Component<
  LibraryErrorBoundaryProps,
  LibraryErrorBoundaryState
> {
  constructor(props: LibraryErrorBoundaryProps) {
    super(props);
    this.state = { error: null, resetKey: resolveResetKey(props) };
  }

  static getDerivedStateFromError(error: unknown): { error: unknown } {
    return { error };
  }

  static getDerivedStateFromProps(
    props: LibraryErrorBoundaryProps,
    state: LibraryErrorBoundaryState,
  ): Partial<LibraryErrorBoundaryState> | null {
    const nextKey = resolveResetKey(props);
    if (state.resetKey !== nextKey) {
      if (state.error !== null) {
        return { error: null, resetKey: nextKey };
      }
      return { resetKey: nextKey };
    }
    return null;
  }

  componentDidCatch(): void {
    // Swallow; fallback rendering handles display.
  }

  private reset = (): void => {
    const callbacks = [
      this.props.onRetry,
      this.props.retry,
      this.props.refetch,
      this.props.onReset,
      this.props.reset,
    ].filter((cb): cb is () => void => typeof cb === 'function');
    for (const cb of callbacks) {
      try {
        cb();
      } catch {
        // ignore callback failures
      }
    }
    this.setState({ error: null });
  };

  render(): ReactNode {
    const { error } = this.state;
    if (error === null) {
      return this.props.children;
    }

    const { fallback, fallbackRender, FallbackComponent, className } = this.props;

    if (fallbackRender) {
      return fallbackRender(error);
    }
    if (typeof fallback === 'function') {
      return (fallback as (e: unknown) => ReactNode)(error);
    }
    if (fallback !== undefined) {
      return fallback;
    }
    if (FallbackComponent) {
      return <FallbackComponent error={error} reset={this.reset} />;
    }

    const invalid = isInvalidFilterError(error);

    const clearCallbacks = [
      this.props.clearFilters,
      this.props.clear,
      this.props.onClearFilters,
      this.props.onClear,
      this.props.onResetFilters,
      this.props.resetFilters,
    ].filter((cb): cb is () => void => typeof cb === 'function');

    const clearFilters = () => {
      for (const cb of clearCallbacks) {
        try {
          cb();
        } catch {
          // ignore
        }
      }
    };

    if (invalid) {
      return (
        <section role="alert" aria-live="polite" className={className}>
          <h2>Invalid filters</h2>
          <p>The current filters are invalid and cannot be applied.</p>
          {clearCallbacks.length > 0 ? (
            <button type="button" onClick={clearFilters}>
              Clear filters
            </button>
          ) : null}
        </section>
      );
    }

    return (
      <section role="alert" aria-live="polite" className={className}>
        <h2>Something went wrong</h2>
        <p>The library could not be loaded.</p>
        <button type="button" onClick={this.reset}>
          Retry
        </button>
        {clearCallbacks.length > 0 ? (
          <button type="button" onClick={clearFilters}>
            Clear filters
          </button>
        ) : null}
      </section>
    );
  }
}

export default LibraryErrorBoundary;
