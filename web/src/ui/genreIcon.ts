import type { ComponentType } from 'react';
import {
  BoltIcon,
  CameraIcon,
  CompassIcon,
  EyeIcon,
  FilmIcon,
  FlagIcon,
  GemIcon,
  GhostIcon,
  HatIcon,
  HeartIcon,
  HouseIcon,
  MasksIcon,
  MusicIcon,
  PlanetIcon,
  QuestionIcon,
  ShieldIcon,
  SmileIcon,
  SparkleIcon,
  TempleIcon,
  type IconProps,
} from './icons';

export type GenreIconComponent = ComponentType<IconProps>;

const GENRE_ICONS = new Map<string, GenreIconComponent>([
  ['action', BoltIcon],
  ['adventure', CompassIcon],
  ['animation', SparkleIcon],
  ['comedy', SmileIcon],
  ['crime', ShieldIcon],
  ['documentary', CameraIcon],
  ['drama', MasksIcon],
  ['family', HouseIcon],
  ['fantasy', GemIcon],
  ['history', TempleIcon],
  ['horror', GhostIcon],
  ['music', MusicIcon],
  ['mystery', QuestionIcon],
  ['romance', HeartIcon],
  ['science fiction', PlanetIcon],
  ['sci-fi', PlanetIcon],
  ['scifi', PlanetIcon],
  ['thriller', EyeIcon],
  ['war', FlagIcon],
  ['western', HatIcon],
]);

function normaliseGenre(name: string): string {
  return name.trim().toLowerCase().replace(/\s+/g, ' ');
}

export function genreIcon(name: string | null | undefined): GenreIconComponent {
  if (!name) {
    return FilmIcon;
  }
  return GENRE_ICONS.get(normaliseGenre(name)) ?? FilmIcon;
}
