import { describe, expect, it } from 'vitest';
import { ACTOR_PEOPLE_QUERY, actorPeopleParams } from './peopleQuery';

describe('peopleQuery actor directory', () => {
  it('supplies the role=actor query the endpoint requires', () => {
    expect(ACTOR_PEOPLE_QUERY).toEqual({ role: 'actor' });
    expect(actorPeopleParams()).toEqual({ query: { role: 'actor' } });
  });

  it('hands back a fresh params object so callers cannot corrupt the literal', () => {
    const first = actorPeopleParams();
    expect(first).not.toBe(actorPeopleParams());
    expect(first.query).toBe(ACTOR_PEOPLE_QUERY);
  });
});
