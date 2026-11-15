export type GetPeopleQuery = { role: 'actor' };

export const ACTOR_PEOPLE_QUERY: GetPeopleQuery = { role: 'actor' };

export function actorPeopleParams(): { query: GetPeopleQuery } {
  return { query: ACTOR_PEOPLE_QUERY };
}
