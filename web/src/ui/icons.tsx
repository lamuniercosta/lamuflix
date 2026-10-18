import type { ReactNode, SVGProps } from 'react';

export type IconProps = Omit<SVGProps<SVGSVGElement>, 'width' | 'height'> & {
  size?: number;
};

interface IconFrameProps extends IconProps {
  children: ReactNode;
}

function IconFrame({ size = 24, children, ...rest }: IconFrameProps) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      focusable="false"
      {...rest}
    >
      {children}
    </svg>
  );
}

export function SearchIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-3.5-3.5" />
    </IconFrame>
  );
}

export function FolderPlusIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
      <path d="M12 11v5M9.5 13.5h5" />
    </IconFrame>
  );
}

export function BookmarkIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M6 4h12v16l-6-4-6 4z" />
    </IconFrame>
  );
}

export function BookmarkFilledIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M6 4h12v16l-6-4-6 4z" fill="currentColor" />
    </IconFrame>
  );
}

export function PlayFilledIcon(props: IconProps) {
  return (
    <IconFrame {...props} stroke="none">
      <path d="M7 5v14l12-7z" fill="currentColor" />
    </IconFrame>
  );
}

export function ChevronDownIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="m6 9 6 6 6-6" />
    </IconFrame>
  );
}

export function GridIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <rect x="4" y="4" width="6" height="6" rx="1.5" />
      <rect x="14" y="4" width="6" height="6" rx="1.5" />
      <rect x="4" y="14" width="6" height="6" rx="1.5" />
      <rect x="14" y="14" width="6" height="6" rx="1.5" />
    </IconFrame>
  );
}

export function ListIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M9 6h11M9 12h11M9 18h11" />
      <path d="M4.5 6h.01M4.5 12h.01M4.5 18h.01" />
    </IconFrame>
  );
}

export function CloseIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M6 6l12 12M18 6 6 18" />
    </IconFrame>
  );
}

export function StarFilledIcon(props: IconProps) {
  return (
    <IconFrame {...props} stroke="none">
      <path
        d="M12 3.5l2.6 5.3 5.8.8-4.2 4.1 1 5.8L12 16.9l-5.2 2.6 1-5.8-4.2-4.1 5.8-.8z"
        fill="currentColor"
      />
    </IconFrame>
  );
}

export function RetryIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M4 12a8 8 0 1 0 2.3-5.6" />
      <path d="M4 4v4h4" />
    </IconFrame>
  );
}

export function AlertTriangleIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M10.3 4.9 2.6 18a2 2 0 0 0 1.7 3h15.4a2 2 0 0 0 1.7-3L13.7 4.9a2 2 0 0 0-3.4 0z" />
      <path d="M12 9v4M12 17h.01" />
    </IconFrame>
  );
}

export function LoaderIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M12 3a9 9 0 1 0 9 9" />
    </IconFrame>
  );
}

export function CopyIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <rect x="9" y="9" width="11" height="11" rx="2" />
      <path d="M5 15V5a2 2 0 0 1 2-2h10" />
    </IconFrame>
  );
}

export function ExternalLinkIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M14 4h6v6M20 4l-9 9" />
      <path d="M18 14v4a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4" />
    </IconFrame>
  );
}

export function ArrowLeftIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M19 12H5M11 6l-6 6 6 6" />
    </IconFrame>
  );
}

export function ArrowRightIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M5 12h14M13 6l6 6-6 6" />
    </IconFrame>
  );
}

export function AdjustmentsIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h10M18 18h2" />
      <circle cx="16" cy="6" r="2" />
      <circle cx="8" cy="12" r="2" />
      <circle cx="16" cy="18" r="2" />
    </IconFrame>
  );
}

export function BoltIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M13 3 5 13h6l-1 8 8-10h-6z" />
    </IconFrame>
  );
}

export function CompassIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <circle cx="12" cy="12" r="9" />
      <path d="M15.5 8.5l-2 5-5 2 2-5z" />
    </IconFrame>
  );
}

export function SparkleIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M12 3c.6 4.6 2.4 6.4 7 7-4.6.6-6.4 2.4-7 7-.6-4.6-2.4-6.4-7-7 4.6-.6 6.4-2.4 7-7z" />
    </IconFrame>
  );
}

export function SmileIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <circle cx="12" cy="12" r="9" />
      <path d="M9 10h.01M15 10h.01" />
      <path d="M8.5 14.5a4.5 4.5 0 0 0 7 0" />
    </IconFrame>
  );
}

export function ShieldIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M12 3l7 3v5c0 4.5-3 8-7 10-4-2-7-5.5-7-10V6z" />
    </IconFrame>
  );
}

export function CameraIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <rect x="3" y="7" width="13" height="10" rx="2" />
      <path d="M16 11l5-3v8l-5-3" />
    </IconFrame>
  );
}

export function MasksIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M5 4h14v8a7 7 0 0 1-14 0z" />
      <path d="M9 9h.01M15 9h.01" />
      <path d="M9 14c1.5 1.2 4.5 1.2 6 0" />
    </IconFrame>
  );
}

export function HouseIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M3 11l9-7 9 7v8a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1z" />
      <path d="M10 20v-5h4v5" />
    </IconFrame>
  );
}

export function GemIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M6 4h12l3 5-9 11L3 9z" />
      <path d="M3 9h18" />
    </IconFrame>
  );
}

export function TempleIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M3 21h18M4 10h16M3 7l9-4 9 4M6 10v8M10 10v8M14 10v8M18 10v8" />
    </IconFrame>
  );
}

export function GhostIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M6 20V11a6 6 0 0 1 12 0v9l-2.5-2-3.5 2-3.5-2L6 20z" />
      <path d="M9.5 11h.01M14.5 11h.01" />
    </IconFrame>
  );
}

export function MusicIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M9 18V6l10-2v12" />
      <circle cx="6.5" cy="18" r="2.5" />
      <circle cx="16.5" cy="16" r="2.5" />
    </IconFrame>
  );
}

export function QuestionIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <circle cx="12" cy="12" r="9" />
      <path d="M9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.6.3-1 .9-1 1.7V14" />
      <path d="M12 17.5h.01" />
    </IconFrame>
  );
}

export function HeartIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10z" />
    </IconFrame>
  );
}

export function PlanetIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <circle cx="12" cy="12" r="5" />
      <ellipse cx="12" cy="12" rx="10" ry="4" transform="rotate(-20 12 12)" />
    </IconFrame>
  );
}

export function EyeIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M2 12s3.5-6 10-6 10 6 10 6-3.5 6-10 6-10-6-10-6z" />
      <circle cx="12" cy="12" r="2.5" />
    </IconFrame>
  );
}

export function FlagIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <path d="M5 21V4M5 4h11l-2 3.5L16 11H5" />
    </IconFrame>
  );
}

export function HatIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <ellipse cx="12" cy="13" rx="9" ry="2.5" />
      <path d="M7.5 12.5V8.5c0-1.5 2-2 4.5-2s4.5.5 4.5 2v4" />
    </IconFrame>
  );
}

export function FilmIcon(props: IconProps) {
  return (
    <IconFrame {...props}>
      <rect x="3" y="4" width="18" height="16" rx="2" />
      <path d="M7 4v16M17 4v16M3 9h4M3 15h4M17 9h4M17 15h4" />
    </IconFrame>
  );
}
