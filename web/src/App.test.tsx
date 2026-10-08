import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('App readiness screen', () => {
  it('renders a main landmark with the LamuFlix heading and readiness paragraph', () => {
    // arrange
    render(<App />);

    // assert
    expect(screen.getByRole('main')).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 1, name: 'LamuFlix' })).toBeInTheDocument();
    expect(screen.getByRole('paragraph')).toHaveTextContent('Web client is ready.');
  });

  it('styles the readiness screen with Tailwind utility classes', () => {
    // arrange
    render(<App />);

    // assert
    expect(screen.getByRole('main')).toHaveClass('min-h-screen', 'bg-slate-950');
    expect(screen.getByRole('heading', { level: 1, name: 'LamuFlix' })).toHaveClass('text-4xl');
    expect(screen.getByRole('paragraph')).toHaveClass('text-lg');
  });
});
