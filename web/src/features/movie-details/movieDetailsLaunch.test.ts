import { describe, expect, it } from 'vitest';
import {
  LAUNCH_FAILED_COPY,
  LAUNCH_NOT_FOUND_COPY,
  LAUNCH_STARTED_COPY,
  LAUNCH_UNAVAILABLE_COPY,
  localLaunchOutcome,
} from './movieDetailsLaunch';

describe('localLaunchOutcome', () => {
  it('reports started on 204', () => {
    expect(localLaunchOutcome(204)).toEqual({ kind: 'started', message: LAUNCH_STARTED_COPY });
  });

  it('reports removal on 404', () => {
    expect(localLaunchOutcome(404)).toEqual({
      kind: 'not-found',
      message: LAUNCH_NOT_FOUND_COPY,
    });
  });

  it('reports the disabled server on 403', () => {
    expect(localLaunchOutcome(403)).toEqual({
      kind: 'unavailable',
      message: LAUNCH_UNAVAILABLE_COPY,
    });
  });

  it.each([409, 422, 400, 500, undefined])('keeps the generic launch failure for %s', (status) => {
    expect(localLaunchOutcome(status)).toEqual({ kind: 'failed', message: LAUNCH_FAILED_COPY });
  });
});
