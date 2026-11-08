import { readFileSync, writeFileSync } from 'node:fs';
import { expect, test, type APIRequestContext } from '@playwright/test';
import type { components } from '../src/api/types';

interface E2EContext {
  baseURL: string;
  seedTitles: string[];
  waitBudgetMs: number;
}

type BrowseResponse = components['schemas']['BrowseMoviesResponse'];

function loadContext(): E2EContext {
  const file = process.env.E2E_CONTEXT_FILE;
  if (!file) {
    throw new Error('E2E_CONTEXT_FILE is required; run the suite through npm run e2e');
  }
  return JSON.parse(readFileSync(file, 'utf8')) as E2EContext;
}

const context = loadContext();

async function findMovieId(request: APIRequestContext, title: string): Promise<number> {
  const response = await request.get(
    `${context.baseURL}/api/movies?sort=Title&direction=Ascending&page=1&pageSize=20`,
  );
  expect(response.ok()).toBe(true);
  const browse = (await response.json()) as BrowseResponse;
  const movie = browse.items.find((item) => item.title === title);
  expect(movie, `seeded movie ${title} is browsable`).toBeDefined();
  return Number(movie!.id);
}

interface SeekObservation {
  duration: number;
  width: number;
  height: number;
  beforeSeek: number;
  seekTarget: number;
  afterSeek: number;
  afterAdvance: number;
  mediaError: string | null;
}

test.describe('stream browser seek', () => {
  test('SMOKE-DEV-456-SEEK playback advances after a seek through the nginx stream URL', async ({
    page,
    request,
  }) => {
    test.setTimeout(context.waitBudgetMs);

    const title = context.seedTitles[0];
    const movieId = await findMovieId(request, title);
    const streamUrl = `${context.baseURL}/api/movies/${movieId}/stream`;

    await page.goto(context.baseURL);

    const streamRequests: { url: string; status: number; range: string | null }[] = [];
    page.on('response', (response) => {
      if (response.url().includes(`/api/movies/${movieId}/stream`)) {
        streamRequests.push({
          url: response.url(),
          status: response.status(),
          range: response.request().headers()['range'] ?? null,
        });
      }
    });

    const observation = await page.evaluate(async (url) => {
      const media = document.createElement('video');
      media.muted = true;
      media.preload = 'auto';
      media.src = url;
      media.style.display = 'none';
      document.body.appendChild(media);

      const result: SeekObservation = {
        duration: 0,
        width: 0,
        height: 0,
        beforeSeek: 0,
        seekTarget: 0,
        afterSeek: 0,
        afterAdvance: 0,
        mediaError: null,
      };

      try {
        await new Promise<void>((resolve, reject) => {
          media.addEventListener('loadedmetadata', () => resolve(), { once: true });
          media.addEventListener(
            'error',
            () => reject(new Error('media error before playback started')),
            { once: true },
          );
          setTimeout(() => reject(new Error('metadata timeout')), 15000);
        });

        result.duration = media.duration;
        result.width = media.videoWidth;
        result.height = media.videoHeight;

        await media.play();
        await new Promise<void>((resolve) => setTimeout(resolve, 600));
        result.beforeSeek = media.currentTime;

        const target = Math.min(result.duration * 0.7, result.duration - 0.5);
        result.seekTarget = target;
        media.currentTime = target;
        await new Promise<void>((resolve, reject) => {
          media.addEventListener('seeked', () => resolve(), { once: true });
          setTimeout(() => reject(new Error('seek timeout')), 15000);
        });
        result.afterSeek = media.currentTime;

        await new Promise<void>((resolve) => setTimeout(resolve, 700));
        result.afterAdvance = media.currentTime;
        media.pause();
      } catch (error) {
        result.mediaError = error instanceof Error ? error.message : String(error);
      } finally {
        media.remove();
      }

      return result;
    }, streamUrl);

    expect(observation.mediaError, 'the sample MP4 must decode in the browser').toBeNull();
    expect(
      observation.duration,
      'the streamed sample must report a finite duration',
    ).toBeGreaterThan(1);
    expect(observation.width, 'the streamed sample must expose a video track').toBeGreaterThan(0);
    expect(observation.beforeSeek, 'playback must have started before the seek').toBeGreaterThan(0);
    expect(
      observation.afterSeek,
      'the seek must land at the requested position',
    ).toBeGreaterThanOrEqual(observation.seekTarget - 0.5);
    expect(observation.afterAdvance, 'playback must advance after the seek').toBeGreaterThan(
      observation.afterSeek,
    );

    expect(
      streamRequests.length,
      'the browser must fetch the stream URL through nginx',
    ).toBeGreaterThan(0);
    expect(
      streamRequests.every((entry) => entry.status === 200 || entry.status === 206),
      `every stream request must succeed, saw ${JSON.stringify(streamRequests)}`,
    ).toBe(true);

    const evidenceFile = process.env.E2E_SEEK_EVIDENCE_FILE;
    if (evidenceFile) {
      writeFileSync(
        evidenceFile,
        `${JSON.stringify(
          {
            streamUrl,
            sourceTitle: title,
            movieId,
            transport: 'web nginx /api proxy at http://localhost:3000',
            observation,
            streamRequests,
          },
          null,
          2,
        )}\n`,
      );
    }
  });
});
