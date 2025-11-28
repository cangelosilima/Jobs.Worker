import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  CircularProgress,
  Alert,
} from '@mui/material';
import {
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  PlayArrow as PlayArrowIcon,
  Work as WorkIcon,
} from '@mui/icons-material';
import { dashboardApi } from '@/api/jobs.api';
import { signalRService, MetricsUpdate } from '@/services/signalr';
import { StatsCard } from './components/StatsCard';
import { ExecutionTrendsChart } from './components/ExecutionTrendsChart';
import { TopFailingJobs } from './components/TopFailingJobs';
import { StaleJobsList } from './components/StaleJobsList';
import { UpcomingSchedules } from './components/UpcomingSchedules';

const Dashboard = () => {
  const {
    data: stats,
    isLoading: statsLoading,
    error: statsError,
    refetch: refetchStats,
  } = useQuery({
    queryKey: ['dashboardStats'],
    queryFn: dashboardApi.getStats,
  });

  const {
    data: trends,
    isLoading: trendsLoading,
  } = useQuery({
    queryKey: ['executionTrends'],
    queryFn: () => dashboardApi.getExecutionTrends(7),
  });

  const {
    data: topFailingJobs,
  } = useQuery({
    queryKey: ['topFailingJobs'],
    queryFn: () => dashboardApi.getTopFailingJobs(10),
  });

  const {
    data: staleJobs,
  } = useQuery({
    queryKey: ['staleJobs'],
    queryFn: () => dashboardApi.getStaleJobs(7),
  });

  const {
    data: upcomingSchedules,
  } = useQuery({
    queryKey: ['upcomingSchedules'],
    queryFn: () => dashboardApi.getUpcomingSchedules(24),
  });

  // Subscribe to real-time metrics updates
  useEffect(() => {
    const handleMetricsUpdate = (data: MetricsUpdate) => {
      refetchStats();
    };

    signalRService.on('MetricsUpdated', handleMetricsUpdate);

    return () => {
      signalRService.off('MetricsUpdated', handleMetricsUpdate);
    };
  }, [refetchStats]);

  if (statsLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  if (statsError) {
    return (
      <Alert severity="error">
        Error loading dashboard: {statsError instanceof Error ? statsError.message : 'Unknown error'}
      </Alert>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Dashboard
      </Typography>

      {/* Stats Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <StatsCard
            title="Total Jobs"
            value={stats?.totalJobs || 0}
            icon={<WorkIcon />}
            color="primary"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatsCard
            title="Active Jobs"
            value={stats?.activeJobs || 0}
            icon={<CheckCircleIcon />}
            color="success"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatsCard
            title="Running Now"
            value={stats?.runningExecutions || 0}
            icon={<PlayArrowIcon />}
            color="info"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatsCard
            title="Failed Today"
            value={stats?.failedToday || 0}
            icon={<ErrorIcon />}
            color="error"
          />
        </Grid>
      </Grid>

      {/* Success Rate Card */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Success Rate (24h)
              </Typography>
              <Typography variant="h3" component="div" color="success.main">
                {stats?.successRatePercentage.toFixed(1)}%
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                {stats?.succeededToday || 0} succeeded / {(stats?.succeededToday || 0) + (stats?.failedToday || 0)} total
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Avg Execution Time
              </Typography>
              <Typography variant="h3" component="div">
                {stats?.averageExecutionTimeSeconds.toFixed(1)}s
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                Average across all executions
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography color="text.secondary" gutterBottom>
                Issues
              </Typography>
              <Typography variant="h3" component="div" color="warning.main">
                {(stats?.delayedOrSkipped || 0) + (stats?.exceedingExpectedDuration || 0)}
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                {stats?.delayedOrSkipped || 0} delayed, {stats?.exceedingExpectedDuration || 0} slow
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Execution Trends Chart */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Execution Trends (7 Days)
              </Typography>
              {trendsLoading ? (
                <Box display="flex" justifyContent="center" p={4}>
                  <CircularProgress />
                </Box>
              ) : (
                <ExecutionTrendsChart data={trends || []} />
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Lists */}
      <Grid container spacing={3}>
        <Grid item xs={12} md={6}>
          <TopFailingJobs jobs={topFailingJobs || []} />
        </Grid>
        <Grid item xs={12} md={6}>
          <StaleJobsList jobs={staleJobs || []} />
        </Grid>
        <Grid item xs={12}>
          <UpcomingSchedules schedules={upcomingSchedules || []} />
        </Grid>
      </Grid>
    </Box>
  );
};

export default Dashboard;
