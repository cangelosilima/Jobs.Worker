import {
  Card,
  CardContent,
  Typography,
  List,
  ListItem,
  ListItemText,
  Chip,
  Box,
} from '@mui/material';
import { Error as ErrorIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { TopFailingJobResponse } from '@/api/types';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';

dayjs.extend(relativeTime);

interface TopFailingJobsProps {
  jobs: TopFailingJobResponse[];
}

export const TopFailingJobs: React.FC<TopFailingJobsProps> = ({ jobs }) => {
  const navigate = useNavigate();

  if (jobs.length === 0) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Top Failing Jobs
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 2 }}>
            No failing jobs found
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Top Failing Jobs
        </Typography>
        <List dense>
          {jobs.map((job) => (
            <ListItem
              key={job.jobId}
              sx={{
                cursor: 'pointer',
                '&:hover': { bgcolor: 'action.hover' },
                borderRadius: 1,
                mb: 0.5,
              }}
              onClick={() => navigate(`/jobs/${job.jobId}`)}
            >
              <Box sx={{ display: 'flex', alignItems: 'center', mr: 2 }}>
                <ErrorIcon color="error" fontSize="small" />
              </Box>
              <ListItemText
                primary={job.jobName}
                secondary={
                  <>
                    <Typography component="span" variant="body2" color="text.secondary">
                      Last failure: {dayjs(job.lastFailureTime).fromNow()}
                    </Typography>
                    {job.lastErrorMessage && (
                      <>
                        <br />
                        <Typography
                          component="span"
                          variant="caption"
                          color="error"
                          sx={{
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            display: '-webkit-box',
                            WebkitLineClamp: 1,
                            WebkitBoxOrient: 'vertical',
                          }}
                        >
                          {job.lastErrorMessage}
                        </Typography>
                      </>
                    )}
                  </>
                }
              />
              <Chip
                label={`${job.failureCount} failures`}
                size="small"
                color="error"
                variant="outlined"
              />
            </ListItem>
          ))}
        </List>
      </CardContent>
    </Card>
  );
};
