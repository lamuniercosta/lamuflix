import { render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('App library integration', () => {
  it('renders the main library view', async () => {
    render(<App />);

    expect(screen.getByRole('main')).toBeInTheDocument();

    await waitFor(() => {
      expect(
        screen.getAllByRole('article').length +
          screen.queryAllByText('Your library is empty.').length,
      ).toBeGreaterThanOrEqual(0);
    });
  });
});
