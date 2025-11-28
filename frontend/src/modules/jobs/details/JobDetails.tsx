import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Typography,
  Card,
  CardContent,
  Grid,
  Chip,
  CircularProgress,
  Alert,
  Divider,
} from '@mui/material';
import { jobsApi } from '@/api/jobs.api';
import { JobStatus } from '@/api/types';

const getStatusLabel = (status: JobStatus): string => {
  switch (status) {
    case JobStatus.Active:
      return 'Active';
    case JobStatus.Disabled:
      return 'Disabled';
    case JobStatus.Archived:
      return 'Archived';
    case JobStatus.Draft:
      return 'Draft';
    default:
      return 'Unknown';
  }
};

const JobDetails = () => {
  const { id } = useParams<{ id: string }>();

  const {
    data: job,
    isLoading,
    error,
  } = useQuery({
    queryKey: ['job', id],
    queryFn: () => jobsApi.getById(id!),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error">
        Error loading job: {error instanceof Error ? error.message : 'Unknown error'}
      </Alert>
    );
  }

  if (!job) {
    return <Alert severity="warning">Job not found</Alert>;
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">{job.name}</Typography>
        <Chip
          label={getStatusLabel(job.status)}
          color={job.status === JobStatus.Active ? 'success' : 'default'}
        />
      </Box>

      <Grid container spacing={3}>
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Job Information
              </Typography>
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Description
                  </Typography>
                  <Typography>{job.description}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Assembly
                  </Typography>
                  <Typography sx={{ fontFamily: 'monospace' }}>{job.assemblyName}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Class
                  </Typography>
                  <Typography sx={{ fontFamily: 'monospace' }}>{job.className}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Method
                  </Typography>
                  <Typography sx={{ fontFamily: 'monospace' }}>{job.methodName}</Typography>
                </Grid>
                {job.timeoutSeconds && (
                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" color="text.secondary">
                      Timeout
                    </Typography>
                    <Typography>{job.timeoutSeconds}s</Typography>
                  </Grid>
                )}
                {job.maxConcurrentExecutions && (
                  <Grid item xs={12} md={6}>
                    <Typography variant="subtitle2" color="text.secondary">
                      Max Concurrent Executions
                    </Typography>
                    <Typography>{job.maxConcurrentExecutions}</Typography>
                  </Grid>
                )}
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        {job.owner && (
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Owner
                </Typography>
                <Typography variant="subtitle2" color="text.secondary">
                  Name
                </Typography>
                <Typography>{job.owner.userName}</Typography>
                <Typography variant="subtitle2" color="text.secondary" sx={{ mt: 1 }}>
                  Email
                </Typography>
                <Typography>{job.owner.email}</Typography>
                {job.owner.teamName && (
                  <>
                    <Typography variant="subtitle2" color="text.secondary" sx={{ mt: 1 }}>
                      Team
                    </Typography>
                    <Typography>{job.owner.teamName}</Typography>
                  </>
                )}
              </CardContent>
            </Card>
          </Grid>
        )}

        {job.retryPolicy && (
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Retry Policy
                </Typography>
                <Typography variant="subtitle2" color="text.secondary">
                  Max Retries
                </Typography>
                <Typography>{job.retryPolicy.maxRetries}</Typography>
                <Typography variant="subtitle2" color="text.secondary" sx={{ mt: 1 }}>
                  Strategy
                </Typography>
                <Typography>{job.retryPolicy.strategy}</Typography>
              </CardContent>
            </Card>
          </Grid>
        )}

        {job.schedules && job.schedules.length > 0 && (
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Schedules
                </Typography>
                <Typography color="text.secondary">{job.schedules.length} schedules configured</Typography>
              </CardContent>
            </Card>
          </Grid>
        )}
      </Grid>
    </Box>
  );
};

export default JobDetails;
