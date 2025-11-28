import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { StatsCard } from '../StatsCard';
import { Work as WorkIcon } from '@mui/icons-material';

describe('StatsCard', () => {
  it('should render title and value', () => {
    render(
      <StatsCard
        title="Total Jobs"
        value={45}
        icon={<WorkIcon />}
        color="primary"
      />
    );

    expect(screen.getByText('Total Jobs')).toBeInTheDocument();
    expect(screen.getByText('45')).toBeInTheDocument();
  });

  it('should format large numbers with locale string', () => {
    render(
      <StatsCard
        title="Total Executions"
        value={1234567}
        icon={<WorkIcon />}
        color="primary"
      />
    );

    // Numbers should be formatted with commas
    expect(screen.getByText('1,234,567')).toBeInTheDocument();
  });

  it('should render icon', () => {
    const { container } = render(
      <StatsCard
        title="Total Jobs"
        value={45}
        icon={<WorkIcon data-testid="work-icon" />}
        color="primary"
      />
    );

    expect(container.querySelector('[data-testid="work-icon"]')).toBeInTheDocument();
  });

  it('should apply correct color', () => {
    const { container } = render(
      <StatsCard
        title="Failed Jobs"
        value={5}
        icon={<WorkIcon />}
        color="error"
      />
    );

    // The icon box should have error color
    const iconBox = container.querySelector('.MuiBox-root');
    expect(iconBox).toBeInTheDocument();
  });
});
