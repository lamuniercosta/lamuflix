import { useRef, useState } from 'react';
import MatchManuallyDialog from './MatchManuallyDialog';

export interface ManualMatchControlProps {
  movieId: number | string;
  libraryClient: import('@tanstack/react-query').QueryClient;
  className: string;
}

export function ManualMatchControl({ movieId, libraryClient, className }: ManualMatchControlProps) {
  const [matchOpen, setMatchOpen] = useState(false);
  const matchTriggerRef = useRef<HTMLButtonElement>(null);
  const closeMatch = () => {
    setMatchOpen(false);
    matchTriggerRef.current?.focus();
  };
  return (
    <>
      <button
        ref={matchTriggerRef}
        type="button"
        className={className}
        aria-haspopup="dialog"
        aria-expanded={matchOpen}
        onClick={() => setMatchOpen(true)}
      >
        Match manually
      </button>
      {matchOpen && (
        <MatchManuallyDialog movieId={movieId} libraryClient={libraryClient} onClose={closeMatch} />
      )}
    </>
  );
}

export default ManualMatchControl;
