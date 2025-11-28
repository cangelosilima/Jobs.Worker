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
import { Warning as WarningIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { StaleJobResponse } from '@/api/types';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';

dayjs.extend(relativeTime);

interface StaleJobsListProps {
  jobs: StaleJobResponse[];
}

export const StaleJobsList: React.FC<StaleJobsListProps> = ({ jobs }) => {
  const navigate = useNavigate();

  if (jobs.length === 0) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Stale Jobs
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 2 }}>
            No stale jobs found
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Stale Jobs
        </Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Jobs that haven't run recently
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
                <WarningIcon color="warning" fontSize="small" />
              </Box>
              <ListItemText
                primary={job.jobName}
                secondary={
                  <>
                    <Typography component="span" variant="body2" color="text.secondary">
                      Last execution: {dayjs(job.lastExecutionTime).fromNow()}
                    </Typography>
                    <br />
                    <Typography component="span" variant="caption" color="warning.main">
                      {job.daysSinceLastExecution} days since last run
                    </Typography>
                  </>
                }
              />
              <Chip
                label={job.isActive ? 'Active' : 'Inactive'}
                size="small"
                color={job.isActive ? 'default' : 'error'}
                variant="outlined"
              />
            </ListItem>
          ))}
        </List>
      </CardContent>
    </Card>
  );
};
