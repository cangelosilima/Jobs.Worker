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
import { Schedule as ScheduleIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { UpcomingScheduleResponse, RecurrenceType } from '@/api/types';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';

dayjs.extend(relativeTime);

interface UpcomingSchedulesProps {
  schedules: UpcomingScheduleResponse[];
}

const getRecurrenceTypeLabel = (type: RecurrenceType): string => {
  switch (type) {
    case RecurrenceType.Daily:
      return 'Daily';
    case RecurrenceType.Weekly:
      return 'Weekly';
    case RecurrenceType.Monthly:
      return 'Monthly';
    case RecurrenceType.MonthlyBusinessDay:
      return 'Monthly (Business Day)';
    case RecurrenceType.Cron:
      return 'Cron';
    case RecurrenceType.OneTime:
      return 'One-time';
    case RecurrenceType.Conditional:
      return 'Conditional';
    default:
      return 'Unknown';
  }
};

export const UpcomingSchedules: React.FC<UpcomingSchedulesProps> = ({ schedules }) => {
  const navigate = useNavigate();

  if (schedules.length === 0) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Upcoming Schedules (Next 24 Hours)
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 2 }}>
            No schedules in the next 24 hours
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" gutterBottom>
          Upcoming Schedules (Next 24 Hours)
        </Typography>
        <List dense>
          {schedules.map((schedule) => (
            <ListItem
              key={schedule.scheduleId}
              sx={{
                cursor: 'pointer',
                '&:hover': { bgcolor: 'action.hover' },
                borderRadius: 1,
                mb: 0.5,
              }}
              onClick={() => navigate(`/jobs/${schedule.jobId}`)}
            >
              <Box sx={{ display: 'flex', alignItems: 'center', mr: 2 }}>
                <ScheduleIcon color="primary" fontSize="small" />
              </Box>
              <ListItemText
                primary={schedule.jobName}
                secondary={
                  <>
                    <Typography component="span" variant="body2" color="text.secondary">
                      Next run: {dayjs(schedule.nextExecutionTime).format('MMM DD, YYYY HH:mm')}
                    </Typography>
                    <br />
                    <Typography component="span" variant="caption" color="text.secondary">
                      {dayjs(schedule.nextExecutionTime).fromNow()} • {getRecurrenceTypeLabel(schedule.recurrenceType)}
                    </Typography>
                  </>
                }
              />
              <Chip
                label={`in ${schedule.hoursUntilExecution.toFixed(1)}h`}
                size="small"
                color={schedule.hoursUntilExecution < 1 ? 'warning' : 'default'}
                variant="outlined"
              />
            </ListItem>
          ))}
        </List>
      </CardContent>
    </Card>
  );
};
