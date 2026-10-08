import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { server } from './server';

const syntheticUrl = 'https://test.local/harness';

describe('MSW synthetic harness', () => {
  it('answers a synthetic URL fetch with the default handler', async () => {
    // act
    const response = await fetch(syntheticUrl);
    const body = (await response.json()) as unknown;

    // assert
    expect(response.status).toBe(200);
    expect(body).toEqual({ source: 'default' });
  });

  it('applies a per-test override for the same synthetic URL', async () => {
    // arrange
    server.use(
      http.get(syntheticUrl, () => HttpResponse.json({ source: 'override' }, { status: 201 })),
    );

    // act
    const response = await fetch(syntheticUrl);
    const body = (await response.json()) as unknown;

    // assert
    expect(response.status).toBe(201);
    expect(body).toEqual({ source: 'override' });
  });

  it('observes the default handler again after the previous test reset', async () => {
    // act
    const response = await fetch(syntheticUrl);
    const body = (await response.json()) as unknown;

    // assert
    expect(response.status).toBe(200);
    expect(body).toEqual({ source: 'default' });
  });
});
